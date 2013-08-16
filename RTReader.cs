﻿using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace LORD2
{
    public static class RTReader
    {
        private static Dictionary<string, Int16> _GlobalI = new Dictionary<string, Int16>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _GlobalOther = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, Int32> _GlobalP = new Dictionary<string, Int32>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _GlobalPLUS = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _GlobalS = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, byte> _GlobalT = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, Int32> _GlobalV = new Dictionary<string, Int32>(StringComparer.OrdinalIgnoreCase);
        private static int _InBEGINCount = 0;
        private static bool _InDOWrite = false;
        private static int _InIFFalse = 999;
        private static bool _InSHOW = false;
        private static string _InWRITEFILE = "";
        private static Random _R = new Random();
        private static Dictionary<string, Dictionary<string, RTRSection>> _RefFiles = new Dictionary<string, Dictionary<string, RTRSection>>(StringComparer.OrdinalIgnoreCase);
        private static int _Version = 2;

        static RTReader()
        {
            // Initialize stuff
            LoadRefFiles(ProcessUtils.StartupPath);

            // Init global variables
            for (int i = 1; i <= 99; i++) _GlobalI.Add("`I" + StringUtils.PadLeft(i.ToString(), '0', 2), 0);
            for (int i = 1; i <= 99; i++) _GlobalP.Add("`P" + StringUtils.PadLeft(i.ToString(), '0', 2), 0);
            for (int i = 1; i <= 99; i++) _GlobalPLUS.Add("`+" + StringUtils.PadLeft(i.ToString(), '0', 2), "");
            for (int i = 1; i <= 10; i++) _GlobalS.Add("`S" + StringUtils.PadLeft(i.ToString(), '0', 2), "");
            for (int i = 1; i <= 99; i++) _GlobalT.Add("`T" + StringUtils.PadLeft(i.ToString(), '0', 2), 0);
            for (int i = 1; i <= 40; i++) _GlobalV.Add("`V" + StringUtils.PadLeft(i.ToString(), '0', 2), 0);

            _GlobalOther.Add("`N", "TODO User Name");
            _GlobalOther.Add("`E", "TODO Enemy Name");
            _GlobalOther.Add("`G", "TODO Graphics Level");
            _GlobalOther.Add("`X", " ");
            _GlobalOther.Add("`D", "\x08");
            _GlobalOther.Add("`1", Ansi.TextColor(Crt.Blue));
            _GlobalOther.Add("`2", Ansi.TextColor(Crt.Green));
            _GlobalOther.Add("`3", Ansi.TextColor(Crt.Cyan));
            _GlobalOther.Add("`4", Ansi.TextColor(Crt.Red));
            _GlobalOther.Add("`5", Ansi.TextColor(Crt.Magenta));
            _GlobalOther.Add("`6", Ansi.TextColor(Crt.Brown));
            _GlobalOther.Add("`7", Ansi.TextColor(Crt.LightGray));
            _GlobalOther.Add("`8", Ansi.TextColor(Crt.White)); // Supposed to be dark gray, but actually white
            _GlobalOther.Add("`9", Ansi.TextColor(Crt.LightBlue));
            _GlobalOther.Add("`0", Ansi.TextColor(Crt.LightGreen));
            _GlobalOther.Add("`!", Ansi.TextColor(Crt.LightCyan));
            _GlobalOther.Add("`@", Ansi.TextColor(Crt.LightRed));
            _GlobalOther.Add("`#", Ansi.TextColor(Crt.LightMagenta));
            _GlobalOther.Add("`$", Ansi.TextColor(Crt.Yellow));
            _GlobalOther.Add("`%", Ansi.TextColor(Crt.White));
            _GlobalOther.Add("`^", Ansi.TextColor(15));
            _GlobalOther.Add("`W", "TODO 1/10s");
            _GlobalOther.Add("`L", "TODO 1/2s");
            _GlobalOther.Add("`\\", "\r\n");
            _GlobalOther.Add("`r0", Ansi.TextBackground(Crt.Black));
            _GlobalOther.Add("`r1", Ansi.TextBackground(Crt.Blue));
            _GlobalOther.Add("`r2", Ansi.TextBackground(Crt.Green));
            _GlobalOther.Add("`r3", Ansi.TextBackground(Crt.Cyan));
            _GlobalOther.Add("`r4", Ansi.TextBackground(Crt.Red));
            _GlobalOther.Add("`r5", Ansi.TextBackground(Crt.Magenta));
            _GlobalOther.Add("`r6", Ansi.TextBackground(Crt.Brown));
            _GlobalOther.Add("`r7", Ansi.TextBackground(Crt.LightGray));
            _GlobalOther.Add("`c", Ansi.ClrScr() + "\r\n\r\n");
            _GlobalOther.Add("`k", "TODO MORE");
        }

        private static void AssignVariable(string variable, string value)
        {
            // Split while we still have the raw input string (in case we're doing a LENGTH operation)
            string[] values = value.Split(' ');

            // Translate the input string
            value = TranslateVariables(value);

            // Check for LENGTH operator
            if ((values.Length == 2) && (values[1].StartsWith("`")))
            {
                // TODO Both of these need to be corrected to match the docs
                if (values[0].ToUpper() == "LENGTH")
                {
                    values[0] = values[1].Length.ToString();
                }
                else if (values[0].ToUpper() == "REALLENGTH")
                {
                    values[0] = values[1].Length.ToString();
                }
            }
            else
            {
                // Translate the first split input variable, which is still raw (and may be used by number variables below)
                values[0] = TranslateVariables(values[0]);
            }

            // See which variables to update
            if (_GlobalI.ContainsKey(variable))
            {
                _GlobalI[variable] = Convert.ToInt16(values[0]);
            }
            if (_GlobalP.ContainsKey(variable))
            {
                _GlobalP[variable] = Convert.ToInt32(values[0]);
            }
            if (_GlobalPLUS.ContainsKey(variable)) _GlobalPLUS[variable] = value;
            if (_GlobalS.ContainsKey(variable)) _GlobalS[variable] = value;
            if (_GlobalT.ContainsKey(variable))
            {
                _GlobalT[variable] = Convert.ToByte(values[0]);
            }
            if (_GlobalV.ContainsKey(variable))
            {
                _GlobalV[variable] = Convert.ToInt32(values[0]);
            }
        }

        public static void DisplayRefFileSections()
        {
            Crt.ClrScr();
            Crt.WriteLn("DEBUG OUTPUT");
            foreach (KeyValuePair<string, Dictionary<string, RTRSection>> RefFile in _RefFiles)
            {
                Crt.WriteLn("Ref File Name: " + RefFile.Key);
                foreach (KeyValuePair<string, RTRSection> Section in RefFile.Value)
                {
                    Crt.WriteLn("  - " + Section.Key + " (" + Section.Value.Script.Count.ToString() + " lines)");
                }
            }
        }

        private static void HandleDO(string[] tokens)
        {
            switch (tokens[1].ToUpper())
            {
                case "GOTO": // @DO GOTO <header or label>
                    // TODO
                    return;
                case "NUMRETURN": // @DO NUMRETURN <int var> <string var>
                    string Translated = TranslateVariables(tokens[3]);
                    string TranslatedWithoutNumbers = Regex.Replace(Translated, "[0-9]", "", RegexOptions.IgnoreCase);
                    AssignVariable(tokens[2], (Translated.Length - TranslatedWithoutNumbers.Length).ToString());
                    return;
                case "READSTRING": // @DO READSTRING <MAX LENGTH> <DEFAULT> <variable TO PUT IT IN> (variable may be left off, in which case store in `S10)
                    // TODO Doesn't take max length or default into account
                    // TODO Maybe need an AssignVariable parameter that tells it not to translate?  Otherwise user input will be translated
                    string Input = "";
                    Crt.ReadLn(out Input);
                    if (tokens.Length >= 5)
                    {
                        AssignVariable(tokens[4], Input);
                    }
                    else
                    {
                        AssignVariable("`S10", Input);
                    }
                    return;
                case "REPLACEALL": // @DO REPLACEALL <find> <replace> <in>
                    AssignVariable(tokens[4], Regex.Replace(TranslateVariables(tokens[4]), Regex.Escape(TranslateVariables(tokens[2])), TranslateVariables(tokens[3]), RegexOptions.IgnoreCase));
                    return;
                case "STRIP": // @DO STRIP <string variable> (really trim)
                    AssignVariable(tokens[2], TranslateVariables(tokens[2]).Trim());
                    return;
                case "STRIPBAD": // @DO STRIPBAD <string variable> (strip illegal ` and replaces via badwords.dat)
                    // TODO
                    return;
                case "TRIM": // @DO TRIM <file name> <number to trim to> (remove lines from file until less than number in length)
                    string FileName = StringUtils.PathCombine(ProcessUtils.StartupPath, TranslateVariables(tokens[2]));
                    int MaxLines = Convert.ToInt32(TranslateVariables(tokens[3]));
                    List<string> Lines = new List<string>();
                    Lines.AddRange(FileUtils.FileReadAllLines(FileName));
                    if (Lines.Count > MaxLines)
                    {
                        while (Lines.Count > MaxLines) Lines.RemoveAt(0);
                        FileUtils.FileWriteAllLines(FileName, Lines.ToArray());
                    }
                    return;
                case "UPCASE": // @DO UPCASE <string variable>
                    AssignVariable(tokens[2], TranslateVariables(tokens[2]).ToUpper());
                    return;
                case "WRITE": // @DO WRITE next one line is written to the screen, no line wrap
                    _InDOWrite = true;
                    return;
                default:
                    switch (tokens[2].ToUpper())
                    {
                        case "-": // @DO <number to change> - <change with what>
                            AssignVariable(tokens[1], (Convert.ToInt32(TranslateVariables(tokens[1])) - Convert.ToInt32(TranslateVariables(tokens[3]))).ToString());
                            return;
                        case "ADD": // DO <string var> ADD <string var or text>
                            AssignVariable(tokens[1], TranslateVariables(tokens[1] + string.Join(" ", tokens, 3, tokens.Length - 3)));
                            return;
                        case "IS": // @DO <Number To Change> IS <Change With What>
                            AssignVariable(tokens[1], string.Join(" ", tokens, 3, tokens.Length - 3));
                            return;
                        case "RANDOM": // @DO <Varible to put # in> RANDOM <Highest number> <number to add to it>
                            int Min = Convert.ToInt32(tokens[4]);
                            int Max = Min + Convert.ToInt32(tokens[3]);
                            AssignVariable(tokens[1], _R.Next(Min, Max).ToString());
                            return;
                    }
                    break;
            }

            Crt.WriteLn("TODO: " + string.Join(" ", tokens));
        }

        private static bool HandleIF(string[] tokens)
        {
            string Left = TranslateVariables(tokens[1]);
            string Right = TranslateVariables(tokens[3]);
            int LeftInt = 0;
            int RightInt = 0;

            switch (tokens[2].ToUpper())
            {
                case "EQUALS": // @IF <Varible> EQUALS <Thing the varible must be, or more or less then, or another varible>
                case "IS": // @IF <Varible> IS <Thing the varible must be, or more or less then, or another varible>
                    if (int.TryParse(Left, out LeftInt) && int.TryParse(Right, out RightInt))
                    {
                        return (LeftInt == RightInt);
                    }
                    else
                    {
                        return (Left == Right);
                    }
                case "EXIST": // @IF <filename> EXIST <true or false>
                    string FileName = StringUtils.PathCombine(ProcessUtils.StartupPath, Left);
                    bool TrueFalse = Convert.ToBoolean(Right.ToUpper());
                    return (File.Exists(FileName) == TrueFalse);
                case "INSIDE": // @IF <Word or variable> INSIDE <Word or variable>
                    return Right.ToUpper().Contains(Left.ToUpper());
                case "LESS": // @IF <Varible> LESS <Thing the varible must be, or more or less then, or another varible>
                    if (int.TryParse(Left, out LeftInt) && int.TryParse(Right, out RightInt))
                    {
                        return (LeftInt < RightInt);
                    }
                    else
                    {
                        throw new ArgumentException("@IF LESS arguments were not numeric");
                    }
                case "MORE": // @IF <Varible> MORE <Thing the varible must be, or more or less then, or another varible>
                    if (int.TryParse(Left, out LeftInt) && int.TryParse(Right, out RightInt))
                    {
                        return (LeftInt > RightInt);
                    }
                    else
                    {
                        throw new ArgumentException("@IF MORE arguments were not numeric");
                    }
                case "NOT": // @IF <Varible> NOT <Thing the varible must be, or more or less then, or another varible>
                    if (int.TryParse(Left, out LeftInt) && int.TryParse(Right, out RightInt))
                    {
                        return (LeftInt != RightInt);
                    }
                    else
                    {
                        return (Left != Right);
                    }
            }

            Crt.WriteLn("TODO: " + string.Join(" ", tokens));
            return false;
        }

        private static void LoadRefFile(string fileName)
        {
            // A place to store all the sections found in this file
            Dictionary<string, RTRSection> Sections = new Dictionary<string, RTRSection>(StringComparer.OrdinalIgnoreCase);

            // Where to store the info for the section we're currently working on
            string CurrentSectionName = "_HEADER";
            RTRSection CurrentSection = new RTRSection();

            // Loop through the file
            string[] Lines = File.ReadAllLines(fileName);
            foreach (string Line in Lines)
            {
                string LineTrimmed = Line.Trim();

                // Check for new section
                if (LineTrimmed.StartsWith("@#"))
                {
                    // Store section in dictionary
                    Sections.Add(CurrentSectionName, CurrentSection);

                    // Get new section name (presumes only one word headers allowed, trims @# off start) and reset script block
                    CurrentSectionName = Line.Trim().Split(' ')[0].Substring(2);
                    CurrentSection = new RTRSection();
                }
                else if (LineTrimmed.StartsWith("@LABEL "))
                {
                    CurrentSection.Script.Add(Line);

                    string[] Tokens = LineTrimmed.Split(' ');
                    CurrentSection.Labels.Add(Tokens[1].ToUpper(), CurrentSection.Script.Count - 1);
                }
                else
                {
                    CurrentSection.Script.Add(Line);
                }
            }

            // Store last open section in dictionary
            Sections.Add(CurrentSectionName, CurrentSection);

            _RefFiles.Add(Path.GetFileNameWithoutExtension(fileName), Sections);
        }

        private static void LoadRefFiles(string directoryName)
        {
            string[] RefFileNames = Directory.GetFiles(directoryName, "*.ref", SearchOption.TopDirectoryOnly);
            foreach (string RefFileName in RefFileNames)
            {
                LoadRefFile(RefFileName);
            }
        }

        public static void RunSection(string fileName, string sectionName)
        {
            // TODO What happens if invalid file and/or section name is given
            Dictionary<string, RTRSection> RefFile = _RefFiles[fileName];
            if (RefFile != null)
            {
                string[] Lines = RefFile[sectionName].Script.ToArray();
                if (Lines != null)
                {
                    RunScript(Lines);
                }
            }
        }

        private static void RunScript(string[] script)
        {
            int LineNumber = 0;
            while (LineNumber < script.Length)
            {
                string Line = script[LineNumber];
                string LineTranslated = TranslateVariables(Line);
                string LineTrimmed = Line.Trim();

                if (_InBEGINCount > _InIFFalse)
                {
                    if (LineTrimmed.StartsWith("@"))
                    {
                        string[] Tokens = LineTrimmed.Split(' ');
                        switch (Tokens[0].ToUpper())
                        {
                            case "@BEGIN":
                                _InBEGINCount += 1;
                                break;
                            case "@END":
                                _InBEGINCount -= 1;
                                break;
                        }
                    }
                }
                else
                {
                    _InIFFalse = 999;

                    if (LineTrimmed.StartsWith("@"))
                    {
                        _InSHOW = false;
                        _InWRITEFILE = "";

                        string[] Tokens = LineTrimmed.Split(' ');
                        switch (Tokens[0].ToUpper())
                        {
                            case "@BEGIN":
                                _InBEGINCount += 1;
                                break;
                            case "@CLOSESCRIPT":
                                return;
                            case "@DISPLAYFILE": // @DISPLAYFILE <filename> <options> (options are NOPAUSE and NOSKIP, separated by space if both used)
                                // TODO As with WRITEFILE, don't allow for ..\..\blah
                                // TODO Handle variables as filename (ie `s02)
                                // TODO Handle NOPAUSE and NOSKIP parameters
                                Ansi.Write(FileUtils.FileReadAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, TranslateVariables(Tokens[1]))));
                                break;
                            case "@DO": // @DO has waaaaaay too many variants
                                HandleDO(Tokens);
                                break;
                            case "@END":
                                _InBEGINCount -= 1;
                                break;
                            case "@IF": // @IF <Varible> <Math> <Thing the varible must be, or more or less then, or another varible>  (Possible math functions: EQUALS, MORE, LESS, NOT)
                                bool Result = HandleIF(Tokens);

                                // Check if it's an IF block, or inline IF
                                // TODO This isn't ideal -- what if the next line is a blank or a comment or something
                                if (script[LineNumber + 1].Trim().ToUpper().StartsWith("@BEGIN"))
                                {
                                    if (!Result)
                                    {
                                        _InIFFalse = _InBEGINCount;
                                        _InBEGINCount += 1;
                                        LineNumber += 1;
                                    }
                                }
                                else
                                {
                                    if (Result) Crt.WriteLn("TODO: " + Line);
                                }
                                break;
                            case "@LABEL": // @LABEL <label name>
                                // Ignore
                                break;
                            case "@SHOW": // @SHOW following lines until next one starting with @ are output to screen
                                _InSHOW = true;
                                break;
                            case "@VERSION": // @VERSION <Version the script needs>
                                int RequiredVersion = Convert.ToInt32(Tokens[1]);
                                if (RequiredVersion > _Version) throw new ArgumentOutOfRangeException("VERSION", "@VERSION requested version " + RequiredVersion + ", we only support version " + _Version);
                                break;
                            case "@WRITEFILE": // @WRITEFILE <filename> following lines until next one starting with @ are output to file (append on existing, create on new)
                                // TODO Strip out any invalid filename characters?  (so for example they can't say ..\..\..\..\windows\system32\important_file.ext)
                                // TODO Handle variables as filename (ie `s02)
                                _InWRITEFILE = StringUtils.PathCombine(ProcessUtils.StartupPath, TranslateVariables(Tokens[1]));
                                break;
                            default:
                                Crt.WriteLn("TODO Unknown: " + LineTrimmed);
                                break;
                        }
                    }
                    else
                    {
                        // TODO If we're outputting something, we might need to do something here
                        if (_InDOWrite)
                        {
                            Ansi.Write(LineTranslated);
                            _InDOWrite = false;
                        }
                        else if (_InSHOW)
                        {
                            Ansi.Write(LineTranslated + "\r\n");
                        }
                        else if (_InWRITEFILE != "")
                        {
                            FileUtils.FileAppendAllText(_InWRITEFILE, LineTranslated + Environment.NewLine);
                        }
                    }
                }

                LineNumber += 1;
            }
        }

        private static string TranslateVariables(string input)
        {
            if (input.Contains("`"))
            {
                string inputUpper = input.ToUpper();

                if (inputUpper.Contains("`I"))
                {
                    foreach (KeyValuePair<string, short> KVP in _GlobalI)
                    {
                        input = Regex.Replace(input, Regex.Escape(KVP.Key), KVP.Value.ToString(), RegexOptions.IgnoreCase);
                    }
                }
                if (inputUpper.Contains("`P"))
                {
                    foreach (KeyValuePair<string, int> KVP in _GlobalP)
                    {
                        input = Regex.Replace(input, Regex.Escape(KVP.Key), KVP.Value.ToString(), RegexOptions.IgnoreCase);
                    }
                }
                if (inputUpper.Contains("`+"))
                {
                    foreach (KeyValuePair<string, string> KVP in _GlobalPLUS)
                    {
                        input = Regex.Replace(input, Regex.Escape(KVP.Key), KVP.Value, RegexOptions.IgnoreCase);
                    }
                }
                if (inputUpper.Contains("`S"))
                {
                    foreach (KeyValuePair<string, string> KVP in _GlobalS)
                    {
                        input = Regex.Replace(input, Regex.Escape(KVP.Key), KVP.Value, RegexOptions.IgnoreCase);
                    }
                }
                if (inputUpper.Contains("`T"))
                {
                    foreach (KeyValuePair<string, byte> KVP in _GlobalT)
                    {
                        input = Regex.Replace(input, Regex.Escape(KVP.Key), KVP.Value.ToString(), RegexOptions.IgnoreCase);
                    }
                }
                if (inputUpper.Contains("`V"))
                {
                    foreach (KeyValuePair<string, int> KVP in _GlobalV)
                    {
                        input = Regex.Replace(input, Regex.Escape(KVP.Key), KVP.Value.ToString(), RegexOptions.IgnoreCase);
                    }
                }
                foreach (KeyValuePair<string, string> KVP in _GlobalOther)
                {
                    input = Regex.Replace(input, Regex.Escape(KVP.Key), KVP.Value, RegexOptions.IgnoreCase);
                }
            }

            // TODO also translate language variables and variable symbols
            return input;
        }
    }
}
