#region # using *.*
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Diagnostics;
using Sokosolver.SokowahnTools;

#endregion

namespace Sokosolver
{
  public partial class Form1 : Form
  {
    static IEnumerable<Tuple<string, Func<string, SokoWahnInterface>>> GetSokowahnInterfaces()
    {
      yield return new Tuple<string, Func<string, SokoWahnInterface>>("2nd Generation", (feld) => new SokoWahn_2nd(feld));
      yield return new Tuple<string, Func<string, SokoWahnInterface>>("3rd Generation", (feld) => new SokoWahn_3rd(feld));
      yield return new Tuple<string, Func<string, SokoWahnInterface>>("3rd Generation List2", (feld) => new SokoWahn_3rd_List2(feld));
      yield return new Tuple<string, Func<string, SokoWahnInterface>>("4th Generation", (feld) => new SokoWahn_4th(feld));
      yield return new Tuple<string, Func<string, SokoWahnInterface>>("4th Generation Byte-Modus", (feld) => new SokoWahn_4th_ByteModus(feld));
      yield return new Tuple<string, Func<string, SokoWahnInterface>>("4th Generation List2", (feld) => new SokoWahn_4th_List2(feld));
      yield return new Tuple<string, Func<string, SokoWahnInterface>>("4th Generation List2 Byte-Modus", (feld) => new SokoWahn_4th_List2_ByteModus(feld));
      yield return new Tuple<string, Func<string, SokoWahnInterface>>("5th Generation", (feld) => new SokoWahn_5th(feld));
    }

    static SokoWahnInterface GetSokowahnInterface(string name, string feldDaten)
    {
      foreach (var satz in GetSokowahnInterfaces())
      {
        if (satz.Item1 == name) return satz.Item2(feldDaten);
      }
      throw new Exception("unbekannt: \"" + name + "\"");
    }

    #region # public Form1()
    public Form1()
    {
      InitializeComponent();
    }
    #endregion

    #region # SokoWahn sokoWahn // Hauptklasse zum berechnen der Spielwelt
    /// <summary>
    /// Hauptklasse zum berechnen der Spielwelt
    /// </summary>
    SokoWahnInterface sokoWahn = null;
    #endregion

    #region # // --- Button "Scan" ---
    private void button1_Click(object sender, EventArgs e)
    {
      if (textBox4.Text.Trim().StartsWith("#"))
      {
        sokoWahn = GetSokowahnInterface(comboBox1.Text, textBox4.Text);
        textBox4.Text = sokoWahn.ToString();
        GC.Collect();
        Text = "scan ok";
        if (!textBox5.Text.StartsWith("-")) textBox5.Text = "-" + textBox5.Text;
        return;
      }
      if (textBox4.Text.TryParse(0).ToString() == textBox4.Text.Trim()) textBox4.Text = "http://www.game-sokoban.com/index.php?mode=level&lid=" + textBox4.Text.TryParse(0);

      if (textBox4.Text.StartsWith("http://www.game-sokoban.com/"))
      {
        string lade = Tools.Download(textBox4.Text.Trim()).ToUtf8String();
        lade = lade.Remove(0, lade.IndexOf("<xml id=\"startLevel\">"));
        lade = lade.Substring(0, lade.IndexOf("</xml>") + 6);
        XElement xElement = XElement.Parse(lade);

        char[] welt = string.Concat(xElement.Descendants("r").Select(x => string.Concat(x.Value.Split(',').Select(z => new string(z[z.Length - 1], z.Substring(0, z.Length - 1).TryParse(1)))) + "\r\n")).Replace('v', ' ').Replace('w', '#').Replace('f', ' ').Replace('a', '.').ToCharArray();
        int spalten = welt.Select((c, i) => new { c, i }).Where(x => x.c == '\r').Select(x => x.i).First();
        xElement.Descendants("b").First().Value.Split(',').Select(z => z.TryParse(0) + (z.TryParse(0) / spalten * 2)).Select(x => welt[x] = welt[x] == '.' ? '*' : '$').Count();
        int spieler = xElement.Descendants("mv").First().Value.TryParse(0);
        spieler += spieler / spalten * 2;
        welt[spieler] = welt[spieler] == '.' ? '+' : '@';

        textBox4.Text = new string(welt); textBox4.Update();

        sokoWahn = GetSokowahnInterface(comboBox1.Text, textBox4.Text);

        textBox4.Text = sokoWahn.ToString();
        GC.Collect();
        return;
      }
    }
    #endregion

    string Schnitt(long gesamt, long teil)
    {
      if (teil == 0) return "";
      return " - " + (((double)teil * 100.0 / (double)gesamt)).ToString("0.000") + "%";
    }

    #region # // --- Button "Next" ---
    private void button2_Click(object sender, EventArgs e)
    {
      button2.Text = "..."; button2.Update();
      if (sokoWahn == null) button1_Click(sender, e);
      if (sokoWahn == null) return;
      int counter = int.Parse(textBox5.Text);
      double mess, mess2;
      mess = mess2 = Zp.TickCount;
      bool speedMode = button4.Text == "...";
    nochmal:
      if (!sokoWahn.Next(counter))
      {
        mess = Zp.TickCount - mess;
        if (counter < 0)
        {
          textBox5.Text = (-counter).ToString();
          button2.Text = "Next";
          return;
        }
        if (sokoWahn.GetType().ToString().Contains("SokoWahn_4th"))
        {
          textBox4.Text = SokowahnStaticTools.LösungswegZuSteps(sokoWahn.GetLösungsweg());
        }
        else
        {
          textBox4.Text = string.Concat(sokoWahn.GetLösungsweg().Select(x => x + "\r\n"));
        }
        if (textBox4.Text == "") textBox4.Text = sokoWahn.ToString();
        timer1.Enabled = false;
        button4.Text = "Auto";
        button2.Text = "Next";
        Text = sokoWahn.KnotenAnzahl.ToString("#,##0") + " - " + sokoWahn.KnotenRest.ToString("#,##0") + " (" + sokoWahn.SuchTiefe + ")" + Schnitt(sokoWahn.KnotenAnzahl, sokoWahn.KnotenRest) + " - " + mess.ToString("#,##0.0") + " ms";
        return;
      }
      else
      {
        if (speedMode && Zp.TickCount - mess < 100.0) goto nochmal;
        mess = Zp.TickCount - mess;
      }
      textBox4.Text = sokoWahn.ToString();

      string anzeige = sokoWahn.KnotenAnzahl.ToString("#,##0") + " - " + sokoWahn.KnotenRest.ToString("#,##0") + " (" + sokoWahn.SuchTiefe + ")" + Schnitt(sokoWahn.KnotenAnzahl, sokoWahn.KnotenRest) + " - " + mess.ToString("#,##0.0") + " ms";

      mess2 = Zp.TickCount - mess2 - mess;

      button2.Text = "Next";
      Text = anzeige + " / " + mess2.ToString("#,##0.0") + " ms";
      //#if DEBUG
      //      if (counter > 0)
      //      {
      //        counter++;
      //        textBox5.Text = counter.ToString();
      //      }
      //#endif
      Update();
    }
    #endregion

    #region # // --- Button "Refresh" ---
    private void button3_Click(object sender, EventArgs e)
    {
      button3.Text = sokoWahn.Refresh().ToString("#,##0");
      textBox4.Text = sokoWahn.ToString();
      Text = sokoWahn.KnotenAnzahl.ToString("#,##0") + " - " + sokoWahn.KnotenRest.ToString("#,##0") + " (" + (sokoWahn.GetLösungsweg().Count() - 1) + ")" + Schnitt(sokoWahn.KnotenAnzahl, sokoWahn.KnotenRest);
      Update();
    }
    #endregion

    private void Form1_Load(object sender, EventArgs e)
    {
      //      textBox4.Text = @"     #### 
      //     #  ##
      //     # $ #
      //   ### # #
      //  ##     #
      //###   $ ##
      //#  . $### 
      //# @.  #   
      //# #.#$#   
      //#  .  #   
      //#######   ";

      //textBox4.Text = "214";
      //textBox4.Text = "261";
      //textBox4.Text = "216";

#if DEBUG
      textBox5.Text = "-1";
#else
      textBox5.Text = "-200";
#endif
      comboBox2.Text = (string)comboBox2.Items[16];

      comboBox1.Items.AddRange(GetSokowahnInterfaces().Select(x => x.Item1).ToArray());
#if DEBUG
      comboBox1.Text = (string)comboBox1.Items[6];
#else
      comboBox1.Text = (string)comboBox1.Items[6];
#endif
      Text = MaxRamverbrauch.ToString();
    }

    /// <summary>
    /// gibt den maximal erlaubten Ramverbrauch in Bytes an
    /// </summary>
    public long MaxRamverbrauch
    {
      get
      {
        return (long)(comboBox2.Text.Replace("Stop bei ", "").Replace(" GB", "").TryParse(0.0) * 1000000000.0);
      }
    }

    private void textBox4_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Control && e.KeyCode == Keys.A) textBox4.SelectAll();
    }

    private void button4_Click(object sender, EventArgs e)
    {
      if (timer1.Enabled)
      {
        timer1.Enabled = false;
        button4.Text = "Auto";
      }
      else
      {
        timer1.Enabled = true;
        button4.Text = "...";
      }
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
      if (button2.Text == "...") return;

      long verbrauch = Process.GetCurrentProcess().WorkingSet64;
      if (verbrauch > MaxRamverbrauch)
      {
        timer1.Enabled = false;
        button4.Text = "Auto (Ram-Stop)";
        return;
      }

      button2_Click(null, null);
    }

  }
}
