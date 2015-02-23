﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Quantum.Quantum.Controllers;
using Quantum.Quantum;
using System.Windows;
using Quantum.Quantum.Utils;

namespace Quantum.Quantum
{

    interface GameController
    {
        void execute(GameEvent gameEvent);
    }

    class GameEvent
    {
        public readonly QuantumModel model;
        public readonly double deltaTime;
        public readonly QuantumGame game;
        public readonly Graphics graphics;
        public readonly Vector mousePosition;
        public readonly double width, height;
        public readonly Team winner;
        public readonly GeneralsDronesCache dronesCache;

        public GameEvent(QuantumGame game, double deltaTime, Graphics graphics, Team winner, double width, double height)
        {
            this.game = game;
            this.deltaTime = deltaTime;
            this.graphics = graphics;
            this.width = width;
            this.height = height;
            this.winner = winner;


            this.model         = game.model;
            this.mousePosition = game.mousePosition;
            this.dronesCache   = game.dronesCache;
            
        }

        public Boolean isButtonPressed(Keys key)
        {
            if(winner != Team.neutral) return false;
            return game.isButtonPressed(key);
        }

        public Boolean isButtonPressed(MouseButtons key)
        {
            if(winner != Team.neutral) return false;
            return game.isButtonPressed(key);
        }
    }


    class QuantumGame
    {


        private readonly Dictionary<object, bool> keyTable = new Dictionary<object, bool>();
        public Vector mousePosition {get; set;}
        public readonly QuantumModel model = new QuantumModel();
        public readonly GeneralsDronesCache dronesCache = new GeneralsDronesCache(50);

        private DeltaTimeCounter deltaTimeCounter;
        private bool firstExecution = true;


        private Brush backgroundBrush = new SolidBrush(Color.FromArgb(unchecked((int)0xff444465)));


        private List<GameController> controllers = new List<GameController>();

        private QuantumMapBuilder mapBuilder = new QuantumMapBuilder();

        private Image greenGameOver = Image.FromFile(@"Resources\green-game-over.png");
        private Image blueGameOver  = Image.FromFile(@"Resources\blue-game-over.png");

        private void initialize(double width, double height)
        {


            controllers.Add(new OutpostConquestController());
            controllers.Add(new GeneralController(Keys.W,  Keys.S,    Keys.A,    Keys.D,     Team.green));
            controllers.Add(new GeneralController(Keys.Up, Keys.Down, Keys.Left, Keys.Right, Team.blue));
            controllers.Add(new DroneOrderingController(Team.green, Keys.Q, MouseButtons.Left));
            controllers.Add(new DroneOrderingController(Team.blue, Keys.ShiftKey, MouseButtons.Right));
            controllers.Add(new DroneController());
            controllers.Add(new DrownRespawnerController());
            controllers.Add(new DronsFighting());
            controllers.Add(new GlobalRender());

            mapBuilder.initializeMap(this, width, height);
            
        }

        private Team checkWinCondition()
        {
            HashSet<Team> outpostHolders = new HashSet<Team>();

            foreach (Outpost outpost in model.Outposts)
            {
                outpostHolders.Add(outpost.Team);
            }

            foreach (General general in model.Generals)
            {
                if (general.Drones.Count > 0)
                {
                    outpostHolders.Add(general.Team);
                }
            }

            if (outpostHolders.Count == 1 && outpostHolders.First() != Team.neutral)
            {
                return outpostHolders.First();
            }

            return Team.neutral;
        }

        public bool playNext(Graphics g, double width, double height)
        {
            try
            {
                DeltaTimeCounter performanceCounter = new DeltaTimeCounter();

                if (firstExecution)
                {
                    deltaTimeCounter = new DeltaTimeCounter();
                    initialize(width, height);
                    firstExecution = false;
                    return false;
                }

                GamePrints.gameIteration++;

                GamePrints.NextFrame();
                long deltaTime = deltaTimeCounter.PrintAndMeasureDelta("Seconds per frame");

                Team winner = checkWinCondition();

                performanceCounter.PrintAndMeasureDelta("Check win condition");

                dronesCache.cacheModel(model);
                performanceCounter.PrintAndMeasureDelta("Cached drones");

                performanceCounter.PrintAndMeasureDelta("Cache drones");

                GameEvent gameEvent = new GameEvent(this, deltaTime / 100000.0, g, winner, width, height);

                foreach (GameController controller in controllers)
                {
                    controller.execute(gameEvent);
                    performanceCounter.PrintAndMeasureDelta(controller.ToString());
                }



                if (winner != Team.neutral)
                {
                    Image gameOverImage = null;
                    gameOverImage = (winner == Team.blue) ? greenGameOver : blueGameOver;

                    if (gameEvent.graphics != null)
                    {
                        gameEvent.graphics.DrawImage(gameOverImage, (int)(width - gameOverImage.Width) / 2, (int)(height - gameOverImage.Height) / 2);
                    }

                    return true;
                }

                performanceCounter.PrintAndMeasureDelta("Done in");
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return false;
        }



        public Boolean isButtonPressed(Object key)
        {   
            return !keyTable.ContainsKey(key) ? false : keyTable[key];
        }

        public void changeInputState(Object key, Boolean pressed) {
            keyTable[key] = pressed;
        }

        internal void create(int port)
        {
            //throw new NotImplementedException();
        }
    }
}
