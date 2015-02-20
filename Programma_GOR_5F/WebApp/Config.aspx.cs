﻿/*
 * Babbi
 * Targhini
 * 5°F
 * Progetto Garden of Raspberries
 * 13-02-15
 * QUASI TERMINATO
 */ 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Gor.Devices;
using Gor;

public partial class ConfigPage : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void btnSalva_Click(object sender, EventArgs e)
    {
        List<Sensor> sensori = new List<Sensor>();
        Adc_MCP3208 converter = new Adc_MCP3208();

        if (chkTemperature.Checked == true && txtIdSens.Text!="")
            sensori.Add(new Temperature_DS1822(chkInSim.Checked, txtIdSens.Text));
        if (chkGroundHumidity.Checked == true)
            sensori.Add(new Humidity_Terrain_YL69YL38(chkInSim.Checked, converter, int.Parse(txtNCT.Text)));
        if (chkAirHumidity.Checked == true)
            sensori.Add(new Humidity_Air_HIH4000(chkInSim.Checked, converter, int.Parse(txtNCUA.Text)));
        if (chkLux.Checked == true)
            sensori.Add(new Light_PhotoResistor(chkInSim.Checked, converter, int.Parse(txtNCLux.Text)));

        //Stringa da cambiare
        using (StreamWriter sw = File.CreateText("C:\\Users\\gianluca.babbi\\Desktop\\Sensori Selezionati"))
        {
            sw.WriteLine();
        }
    }
}