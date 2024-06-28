using CSGSI;
using CSGSI.Nodes;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CS2GSI
{
    internal class GSI
    {
        private int RoundOverFocusDelay = 5;
        private int IntermissionFocusDelay = 12;
        private long MinTabInterval = 8000;

        private GameStateListener gsl;
        private String cs2Class = "SDL_app";
        private String cs2Caption = "Counter-Strike 2";
        private IntPtr hWnd = IntPtr.Zero;
        private long lastTab = 0;
        private bool isIntermission = false;
        private Options[] optionsNew;
        private int lastEvent = -1;
        private bool isPaused = false;

        struct Options
        {
            public String title;
            public bool enabled;
            public int delay;
            public Options(String title, bool enabled)
            {
                this.title = title;
                this.enabled = enabled;
                this.delay = 0;
            }

            public Options(String title, bool enabled, int delay)
            {
                this.title = title;
                this.enabled = enabled;
                this.delay = delay;
            }
        }

        public GSI(String uri)
        {
            this.gsl = new GameStateListener(uri);
            gsl.NewGameState += OnNewGameState;

            optionsNew = new Options[] {
                new Options("[1]freezetime", false),
                new Options("[2]roundover", true, RoundOverFocusDelay),
                new Options("[3]roundstart", true),
                new Options("[4]intermission", true, IntermissionFocusDelay),
                new Options("[5]matchstart", true),
            };
        }

        public bool start()
        {
            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

            hWnd = FindWindow(cs2Class, cs2Caption);

            return (gsl.Start() && hWnd != IntPtr.Zero);
        }

        /**
         * TODO:
         * check if it tabs in after connecting for first round
         * setup console ui to enable/disable certain events
         */ 

        private void OnNewGameState(GameState gs)
        {
            //if (!gs.Map.Mode.ToString().Equals("Competitive")) return;

            isIntermission = gs.Round.Phase.ToString().Equals("Intermission");

            if (!isPaused)
            {
                if (optionsNew[0].enabled) ChangeFocusFreezeTime(gs);
                if (optionsNew[1].enabled) ChangeFocusRoundOver(gs);
                if (optionsNew[2].enabled) ChangeFocusRoundStart(gs);
                if (optionsNew[3].enabled) ChangeFocusIntermission(gs);
                if (optionsNew[4].enabled) ChangeFocusMatchStart(gs);
            }

            DebugPrint(gs);
        }

        [Conditional("DEBUG")]
        private void DebugPrint(GameState gs)
        {
            //warmup -> live: when match first starts
            //live -> intermission: when halftime first starts
            Console.Write(gs.Previously.Map.Phase.ToString() + " -> " + gs.Map.Phase.ToString() + " ");
            //Console.Write(gs.Previously.Round.Phase.ToString() + " -> " + gs.Round.Phase.ToString() + " ");
            //Console.Write(gs.Player.Name + " " + gs.Player.SteamID);
            Console.WriteLine();
            //Console.WriteLine();
        }

        private void WriteOption(int optionNum, String optionName, bool optionOn)
        {
            if (lastEvent == optionNum)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                lastEvent = -1;
            }
            Console.Write(optionName);
            if (optionOn) Console.Write("*");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
        }

        private void WriteConsole()
        {
            Console.Clear();

            if (isPaused) Console.ForegroundColor = ConsoleColor.Green;
            else Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Pause with \"~\"");
            Console.ForegroundColor = ConsoleColor.White;

            for (int i = 0; i < optionsNew.Length; i++)
            {
                WriteOption(i+1, optionsNew[i].title, optionsNew[i].enabled);
            }
        }

        public async void ReadInput()
        {
            ConsoleKeyInfo input = new ConsoleKeyInfo();
            bool wasInvalid = false;

            do
            {
                WriteConsole();
                if (wasInvalid) Console.WriteLine("invalid input");

                input = Console.ReadKey(true);
                switch (input.KeyChar)
                {
                    case '1':
                        optionsNew[0].enabled = !optionsNew[0].enabled;
                        break;
                    case '2':
                        optionsNew[1].enabled = !optionsNew[1].enabled;
                        break;
                    case '3':
                        optionsNew[2].enabled = !optionsNew[2].enabled;
                        break;
                    case '4':
                        optionsNew[3].enabled = !optionsNew[3].enabled;
                        break;
                    case '5':
                        optionsNew[4].enabled = !optionsNew[4].enabled;
                        break;
                    case '`':
                        isPaused = !isPaused;
                        break;
                    default:
                        wasInvalid = true;
                        break;
                }
            } while (true);
        }

        private void ChangeFocusRoundStart(GameState gs)
        {
            if (gs.Previously.Round.Phase.ToString().Equals("FreezeTime") && gs.Round.Phase.ToString().Equals("Live")) {
                ChangeFocus(3);
            }
        }

        private void ChangeFocusIntermission(GameState gs)
        {
            if (gs.Previously.Map.Phase.ToString().Equals("Live") && gs.Map.Phase.ToString().Equals("Intermission"))
                ChangeFocus(optionsNew[3].delay, 4);
        }

        private void ChangeFocusMatchStart(GameState gs)
        {
            if (gs.Previously.Map.Phase.ToString().Equals("Warmup") && gs.Map.Phase.ToString().Equals("Live"))
                ChangeFocus(5);
        }
        
        private void ChangeFocusFreezeTime(GameState gs)
        {
            // change focus when freezetime starts
            if ((gs.Previously.Round.JSON.Contains("phase") &&
                gs.Round.JSON.Contains("phase")) &&
                (gs.Previously.Round.Phase == RoundPhase.Over ||
                gs.Previously.Round.Phase.ToString().Equals("Undefined")) &&
                gs.Round.Phase == RoundPhase.FreezeTime)
            {
                //Console.WriteLine("\nnew round! " + DateTime.Now.Millisecond.ToString() + " " + DateTime.Now.Second.ToString() + "\n" + 
                //gs.Previously.Round.Phase + " " +
                //gs.Round.Phase + " \n");
                ChangeFocus(1);
            }
        }

        private void ChangeFocusRoundOver(GameState gs)
        {
            // change focus x seconds after round ends(when a team wins a round)
            if (gs.Round.JSON.Contains("phase"))
            {
                if (gs.Previously.Round.Phase == RoundPhase.Live && gs.Round.Phase == RoundPhase.Over && !gs.Map.Phase.ToString().Equals("Intermission"))
                    ChangeFocus(optionsNew[1].delay, 2);

            }
        }

        private bool ChangeFocus(int lastEvent)
        {
            //Console.WriteLine("ChangeFocus");

            [DllImport("user32.dll")]
            static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            static extern IntPtr GetForegroundWindow();

            [DllImport("User32.dll")]
            static extern bool ShowWindow(IntPtr handle, int nCmdShow);

            // this gives you the handle of the window you need.
            IntPtr hCurWnd = GetForegroundWindow();

            // then use this handle to bring the window to focus or forground
            // sometimes the window may be minimized and the setforground function cannot bring it to focus so:

            /*use this ShowWindow(IntPtr handle, int nCmdShow);
            *there are various values of nCmdShow 3, 5 ,9. What 9 does is: 
            *Activates and displays the window. If the window is minimized or maximized, *the system restores it to its original size and position. An application *should specify this flag when restoring a minimized window */

            if (hWnd != hCurWnd)
            {
                Console.WriteLine("  focus changed");

                DateTimeOffset dto = new DateTimeOffset(DateTime.Now);
                long unix = dto.ToUnixTimeMilliseconds();

                if (isIntermission) return false;
                if (unix - lastTab < MinTabInterval)
                {
                    //ChangeFocus((int)MinTabInterval / 4, lastEvent);
                    return false;
                }

                lastTab = unix;

                ShowWindow(hWnd, 9);
                SetForegroundWindow(hWnd);

                this.lastEvent = lastEvent;
                WriteConsole();

                return true;
            }
            return false;
        }

        private async void ChangeFocus(int time, int lastEvent)
        {
            //Console.WriteLine("ChangeFocusTimer");
            await Task.Delay(TimeSpan.FromSeconds(time));
            ChangeFocus(lastEvent);
        }

    }
}
