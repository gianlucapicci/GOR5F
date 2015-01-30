/*
This is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or
distribute this software, either in source code form or as a compiled
binary, for any purpose, commercial or non-commercial, and by any
means.

In jurisdictions that recognize copyright laws, the author or authors
of this software dedicate any and all copyright interest in the
software to the public domain. We make this dedication for the benefit
of the public at large and to the detriment of our heirs and
successors. We intend this dedication to be an overt act of
relinquishment in perpetuity of all present and future rights to this
software under copyright law.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

For more information, please refer to <http://unlicense.org/>
*/

/*
This version is for pigpio version 18+
*/

#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#include <unistd.h>
#include <sys/socket.h>
#include <sys/types.h>
#include <netdb.h>
#include <arpa/inet.h>

#include "pigpio.h"
#include "command.h"

/*
This program provides a socket interface to some of
the commands available from pigpio.
*/

char command_buf[8192];
char response_buf[8192];

#define SOCKET_OPEN_FAILED -1

void fatal(char *fmt, ...)
{
   char buf[128];
   va_list ap;

   va_start(ap, fmt);
   vsnprintf(buf, sizeof(buf), fmt, ap);
   va_end(ap);

   fprintf(stderr, "%s\n", buf);

   fflush(stderr);
}

static int openSocket(void)
{
   int sock, err;
   struct addrinfo hints, *res, *rp;
   const char *addrStr, *portStr;

   portStr = getenv(PI_ENVPORT);

   if (!portStr) portStr = PI_DEFAULT_SOCKET_PORT_STR;

   addrStr = getenv(PI_ENVADDR);

   if (!addrStr) addrStr = PI_DEFAULT_SOCKET_ADDR_STR;

   memset (&hints, 0, sizeof (hints));

   hints.ai_family   = PF_UNSPEC;
   hints.ai_socktype = SOCK_STREAM;
   hints.ai_flags   |= AI_CANONNAME;

   err = getaddrinfo(addrStr, portStr, &hints, &res);

   if (err) return SOCKET_OPEN_FAILED;

   for (rp=res; rp!=NULL; rp=rp->ai_next)
   {
      sock = socket(rp->ai_family, rp->ai_socktype, rp->ai_protocol);

      if (sock == -1) continue;

      if (connect(sock, rp->ai_addr, rp->ai_addrlen) != -1) break;
   }

   freeaddrinfo(res);

   if (rp == NULL) return SOCKET_OPEN_FAILED;

   return sock;
}

void print_result(int sock, int rv, cmdCmd_t cmd)
{
   int i, r;
   uint32_t *p;

   r = cmd.res;

   switch (rv)
   {
      case 0:
      case 1:
         if (r < 0)
         {
            printf("%d\n", r);
            fatal("ERROR: %s", cmdErrStr(r));
         }
         break;

      case 2:
         printf("%d\n", r);
         if (r < 0) fatal("ERROR: %s", cmdErrStr(r));
         break;

      case 3:
         printf("%08X\n", cmd.res);
         break;

      case 4:
         printf("%u\n", cmd.res);
         break;

      case 5:
         printf(cmdUsage);
         break;

      case 6: /* I2CPK I2CRD I2CRI I2CRK SERR SLR SPIX SPIR */
         printf("%d", r);
         if (r < 0) fatal("ERROR: %s", cmdErrStr(r));
         if (r > 0)
         {
            for (i=0; i<r; i++)
            {
               printf(" %hhu", response_buf[i]);
            }
         }
         printf("\n");
         break;

      case 7: /* PROCP */
         if (r != (4 + (4*PI_MAX_SCRIPT_PARAMS)))
         {
            printf("%d", r);
            fatal("ERROR: %s", cmdErrStr(r));
         }
         else
         {
            p = (uint32_t *)response_buf;
            printf("%d", p[0]);
            for (i=0; i<PI_MAX_SCRIPT_PARAMS; i++)
            {
               printf(" %d", p[i+1]);
            }
         }
         printf("\n");
         break;
   }
}

void get_extensions(int sock, int command, int res)
{
   switch (command)
   {
      case PI_CMD_I2CPK:
      case PI_CMD_I2CRD:
      case PI_CMD_I2CRI:
      case PI_CMD_I2CRK:
      case PI_CMD_PROCP:
      case PI_CMD_SERR:
      case PI_CMD_SLR:
      case PI_CMD_SPIX:
      case PI_CMD_SPIR:

         if (res > 0)
         {
            recv(sock, response_buf, res, MSG_WAITALL);
            response_buf[res] = 0;
         }
         break;
   }
}

int main(int argc , char *argv[])
{
   int sock, command;
   int idx, i, pp, l, len;
   cmdCmd_t cmd;
   uint32_t p[CMD_P_ARR];
   cmdCtlParse_t ctl;
   cmdScript_t s;
   char v[CMD_MAX_EXTENSION];

   sock = openSocket();

   command_buf[0] = 0;
   l = 0;
   pp = 0;

   for (i=1; i<argc; i++)
   {
      l += (strlen(argv[i]) + 1);
      if (l < sizeof(command_buf))
         {sprintf(command_buf+pp, "%s ", argv[i]); pp=l;}
   }

   if (pp) {command_buf[--pp] = 0;}

   ctl.eaten = 0;

   len = strlen(command_buf);
   idx = 0;

   while ((idx >= 0) && (ctl.eaten < len))
   {
      if ((idx=cmdParse(command_buf, p, CMD_MAX_EXTENSION, v, &ctl)) >= 0)
      {
         command = p[0];

         if (command < PI_CMD_SCRIPT)
         {
            if (command == PI_CMD_HELP)
            {
               printf(cmdUsage);
            }
            else if (command == PI_CMD_PARSE)
            {
               cmdParseScript(v, &s, 1);
               if (s.par) free (s.par);
            }
            else
            {
               cmd.cmd = command;
               cmd.p1 = p[1];
               cmd.p2 = p[2];
               cmd.p3 = p[3];

               if (sock != SOCKET_OPEN_FAILED)
               {
                  if (send(sock, &cmd, sizeof(cmdCmd_t), 0) ==
                     sizeof(cmdCmd_t))
                  {
                     if (p[3]) send(sock, v, p[3], 0); /* send extensions */

                     if (recv(sock, &cmd, sizeof(cmdCmd_t), MSG_WAITALL) ==
                        sizeof(cmdCmd_t))
                     {
                        get_extensions(sock, command, cmd.res);

                        print_result(sock, cmdInfo[idx].rv, cmd);
                     }
                     else fatal("socket receive failed");
                  }
                  else fatal("socket send failed");
               }
               else fatal("socket connect failed");
            }
         }
         else fatal("%s only allowed within a script", cmdInfo[idx].name);
      }
      else
      {
         if (idx == CMD_UNKNOWN_CMD)
            fatal("%s? unknown command, pigs h for help", cmdStr());
         else
            fatal("%s: bad parameter, pigs h for help", cmdStr());
      }
   }

   if (sock >= 0) close(sock);

   return 0;
}

