﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Timers;
using System.Windows.Forms;
using IniParser;
using IniParser.Model;
using Nancy;
using Nancy.Hosting.Self;
using static FoxyListener.Program;
using Timer = System.Timers.Timer;

namespace FoxyListener
{
    static class Program
    {
        public static IniData UstData;
        public static string UstFileStr;
        public static string UstFilePath;
        public static readonly Encoding EncodeJPN = Encoding.GetEncoding("Shift_JIS");
        public static readonly string UstHeader = "[#VERSION]\r\n" + "UST Version 1.20\r\n";

        static void Main(string[] path)
        {
            UstFilePath = string.Join("", path);
            UstFileStr = File.ReadAllText(UstFilePath, EncodeJPN)
                .Replace(UstHeader, "");
            UstData = new FileIniDataParser().Parser.Parse(UstFileStr);

            UstData.Sections.RemoveSection("#PREV");
            UstData.Sections.RemoveSection("#NEXT");
            UstData.Sections.RemoveSection("#SETTING");

            using (NancyHost host = new NancyHost(new HostConfiguration
                    {RewriteLocalhost = true, UrlReservations = new UrlReservations {CreateAutomatically = true}},
                new Uri("http://localhost:2020/")))
            {
                host.Start();

                Console.WriteLine(@"Foxy Listener is running on 2020 port");
                Console.WriteLine(@"Press any [Enter] to close the host.");

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }

            File.WriteAllText(string.Join("", UstFilePath),
                UstHeader + UstData.ToString().Replace(" = ", "=").Replace("\r\n\r\n", "\r\n"), EncodeJPN);
        }
    }

    // ReSharper disable once UnusedMember.Global
    public sealed class HomeModule : NancyModule
    {
        public HomeModule()
        {
            Get("/", x => "Welcome to Foxy");
            Get("/get", x => UstData.ToString());
            Get("/get/count", x => UstData.Sections.Count.ToString());
            Get("/get/full", x => UstFileStr);
            Get("/get/count/nr", x =>
            {
                int i = 0;
                foreach (var itemSection in UstData.Sections)
                {
                    if (itemSection.Keys["Lyric"].ToUpper() == "R") continue;
                    i++;
                }
                return i.ToString();
            });
            Get("/get/names", x =>
            {
                var nameList = new List<string>();
                foreach (var item in UstData.Sections) nameList.Add(item.SectionName);
                return string.Join(Environment.NewLine, nameList);
            });
            Get("/get/lyric", x =>
            {
                var nameList = new List<string>();
                foreach (var item in UstData.Sections) nameList.Add(item.Keys["Lyric"]);
                return string.Join(",", nameList);
            });
            Get("/get/{section}/{key}", x =>
            {
                try
                {
                    return new FileIniDataParser().Parser.Parse(UstFileStr)
                        .Sections["#" + x.section][x.key].ToString();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return e.ToString();
                }
            });
            Get("/set/{section}/{key}/{val}", x =>
            {
                try
                {
                    UstData["#" + x.section][x.key] = x.val.ToString();
                    return "OK";
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return e.ToString();
                }
            });
            Get("/set/{section}/{key}", x =>
            {
                try
                {
                    UstData["#" + x.section][x.key] = Request.Query["val"].ToString();
                    return "OK";
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return e.ToString();
                }
            });
            Get("/set/lyric", x =>
            {
                try
                {
                    int i = 0;
                    var lyrics = ((string) Request.Query["val"].ToString()).Split(',');
                    var rIgn = ((int)Request.Query["r"]);
                    foreach (var itemSection in UstData.Sections)
                    {
                        if (itemSection.Keys["Lyric"].ToUpper() == "R" && rIgn == 1) continue;
                        itemSection.Keys["Lyric"] = lyrics[i];
                        i++;
                    }
                    return "OK";
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return e.ToString();
                }
            });
            Get("/set/lyric/wq", x =>
            {
                try
                {
                    int i = 0;
                    var lyrics = ((string) Request.Query["val"].ToString()).Split(',');
                    var rIgn = ((int) Request.Query["r"]);
                    foreach (var itemSection in UstData.Sections)
                    {
                        if (itemSection.Keys["Lyric"].ToUpper() == "R" && rIgn == 1) continue;
                        itemSection.Keys["Lyric"] = lyrics[i];
                        i++;
                    }

                    File.WriteAllText(UstFilePath,
                        UstHeader + UstData.ToString().Replace(" = ", "=").Replace("\r\n\r\n", "\r\n"), EncodeJPN);
                    Timer t = new Timer(100) {Enabled = true};
                    t.Elapsed += T_Elapsed;
                    return "OK";
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return e.ToString();
                }
            });
            Get("/wq", x =>
            {
                try
                {
                    File.WriteAllText(UstFilePath,
                        UstHeader + UstData.ToString().Replace(" = ", "=").Replace("\r\n\r\n", "\r\n"), EncodeJPN);
                    Timer t = new Timer(100) { Enabled = true };
                    t.Elapsed += T_Elapsed;
                    return "OK";
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return e.ToString();
                }
            });
        }

        private static void T_Elapsed(object sender, ElapsedEventArgs e)
        {
            Application.Exit();
            Environment.Exit(0);
        }
    }
}
