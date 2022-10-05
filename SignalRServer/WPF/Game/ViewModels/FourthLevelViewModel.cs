﻿using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WPF.Connection;
using WPF.Game.AbstractFactory.Classes.StrongMobFactory;
using WPF.Game.AbstractFactory.Classes.WeakMobFactory;
using WPF.Game.AbstractFactory.Interfaces;
using WPF.Game.Classes;
using WPF.Game.Factory.Classes;
using WPF.Game.Factory.Interfaces;
using WPF.Game.Singleton.Classes;
using WPF.Game.Strategy;
using WPF.Views;

namespace WPF.Game.ViewModels
{
    public class FourthLevelViewModel : ViewModelBase
    {
        DispatcherTimer gameTimer = new DispatcherTimer();
        bool goLeft, goRight, goUp, goDown;
        bool noLeft, noRight, noUp, noDown;
        CoinFactory _SilverCoinFactory;
        CoinFactory _GoldCoinFactory;
        HubConnection _connection;
        WeakMobFactory _mobFactory;
        StrongMobFactory _strongMobFactory;
        Pacman pacman;
        Pacman greenPacman;

        private int _yellowPacmanLeft;
        public int YellowPacmanLeft
        {
            get
            {
                return pacman.PacmanLeft;
            }
            private set
            {
                if (value != pacman.PacmanLeft)
                {
                    pacman.PacmanLeft = value;
                    OnPropertyChanged("YellowPacmanLeft");
                }
            }
        }

        private int _yellowPacmanTop;
        public int YellowPacmanTop
        {
            get
            {
                return pacman.PacmanTop;
            }
            private set
            {
                if (value != pacman.PacmanTop)
                {
                    pacman.PacmanTop = value;
                    OnPropertyChanged("YellowPacmanTop");
                }
            }
        }

        private int _greenPacmanLeft;

        public int GreenPacmanLeft
        {
            get
            {
                return greenPacman.PacmanLeft;
            }
            private set
            {
                if (value != greenPacman.PacmanLeft)
                {
                    greenPacman.PacmanLeft = value;
                    OnPropertyChanged("GreenPacmanLeft");
                }
            }
        }

        private int _greenPacmanTop;
        public int GreenPacmanTop
        {
            get
            {
                return greenPacman.PacmanTop;
            }
            private set
            {
                if (value != greenPacman.PacmanTop)
                {
                    greenPacman.PacmanTop = value;
                    OnPropertyChanged("GreenPacmanTop");
                }
            }
        }

        public ObservableCollection<ICoin> Coins { get; set; }
        public ObservableCollection<IGhost> GhostMobs { get; set; }
        public ObservableCollection<IZombie> ZombieMobs { get; set; }
        public ObservableCollection<Apple> Apples { get; set; }
        public List<Apple> ApplesList { get; set; }
        public ObservableCollection<RottenApple> RottenApples { get; set; }
        public List<RottenApple> RottenApplesList { get; set; }
        public ObservableCollection<Cherry> Cherries { get; set; }
        public ObservableCollection<Strawberry> Strawberries { get; set; }

        PacmanHitbox myPacmanHitBox = PacmanHitbox.GetInstance;

        int ghostMoveStep = 130;
        int score = 0;
        int oponentScore = 0;

        public FourthLevelViewModel(IConnectionProvider connectionProvider)
        {
            _GoldCoinFactory = new GoldCoinCreator();
            _SilverCoinFactory = new SilverCoinCreator();
            _mobFactory = new WeakMobFactory();
            _strongMobFactory = new StrongMobFactory();
            _connection = connectionProvider.GetConnection();
            pacman = new Pacman();
            greenPacman = new Pacman();
            ApplesList = new List<Apple>();
            var tempApplesList = ApplesList;
            RottenApplesList = new List<RottenApple>();
            var tempRottenApplesList = RottenApplesList;
            GreenPacmanTop = 20;
            GreenPacmanLeft = 20;
            YellowPacmanLeft = 20;
            YellowPacmanTop = 20;

            Coins = Utils.Utils.GetFirstHalfCoins(_SilverCoinFactory);
            Coins = Utils.Utils.GetSecondHalfCoins(_GoldCoinFactory, Coins);
            GhostMobs = SpawnGhosts();
            ZombieMobs = SpawnZombies();
            Apples = Utils.Utils.CreateApples(ref tempApplesList);
            ApplesList = tempApplesList;
            RottenApples = Utils.Utils.CreateRottenApples(ref tempRottenApplesList);
            RottenApplesList = tempRottenApplesList;
            Cherries = Utils.Utils.CreateCherries();
            Strawberries = Utils.Utils.CreateStrawberries();
            GameSetup();
            ListenServer();
        }
        private ObservableCollection<IGhost> SpawnGhosts()
        {
            ObservableCollection<IGhost> result = new ObservableCollection<IGhost>();
            var firstGhost = _strongMobFactory.CreateGhost(500, 600);
            var secondGhost = _strongMobFactory.CreateGhost(50, 750);
            result.Add(firstGhost);
            result.Add(secondGhost);
            return result;
        }

        private ObservableCollection<IZombie> SpawnZombies()
        {
            ObservableCollection<IZombie> result = new ObservableCollection<IZombie>();
            var firstZombie = _mobFactory.CreateZombie(500, 50);
            var secondZombie = _mobFactory.CreateZombie(300, 300);
            result.Add(firstZombie);
            result.Add(secondZombie);
            return result;
        }

        private void ListenServer()
        {
            _connection.On<string>("OponentCordinates", (serializedObject) =>
            {
                Pacman deserializedObject = JsonSerializer.Deserialize<Pacman>(serializedObject);
                GreenPacmanLeft = deserializedObject.PacmanLeft;
                GreenPacmanTop = deserializedObject.PacmanTop;
            });
        }

        private void GameSetup()
        {
            gameTimer.Tick += GameLoop;
            gameTimer.Interval = TimeSpan.FromMilliseconds(30); ///will tick every 20ms
            gameTimer.Start();
        }

        private async void GameLoop(object? sender, EventArgs e)
        {
            //txtScore.Content = "Score: " + score; TODO bind to score property 
            // show the scoreo to the txtscore label. 

            int AppHeight = (int)Application.Current.MainWindow.Height;
            int AppWidth = (int)Application.Current.MainWindow.Width;
            int oldLeft = YellowPacmanLeft;
            int oldTop = YellowPacmanTop;
            if (goRight)
            {
                YellowPacmanLeft += pacman.Speed;
            }
            if (goLeft)
            {
                YellowPacmanLeft -= pacman.Speed;
            }
            if (goUp)
            {
                YellowPacmanTop -= pacman.Speed;
            }
            if (goDown)
            {
                YellowPacmanTop += pacman.Speed;
            }

            if (oldLeft != YellowPacmanLeft || oldTop != YellowPacmanTop)
            {
                string serializedObject = JsonSerializer.Serialize(pacman);
                await _connection.InvokeAsync("SendPacManCordinates", serializedObject);
            }

            if (goDown && YellowPacmanTop + 280 > AppHeight)
            {
                noDown = true;
                goDown = false;
            }
            if (goUp && YellowPacmanTop < 5)
            {
                noUp = true;
                goUp = false;
            }
            if (goLeft && YellowPacmanLeft - 5 < 1)
            {
                noLeft = true;
                goLeft = false;
            }
            if (goRight && YellowPacmanLeft + 40 > AppWidth)
            {
                noRight = true;
                goRight = false;
            }

            Rect pacmanHitBox = myPacmanHitBox.GetCurrentHitboxPosition(YellowPacmanLeft, YellowPacmanTop, 30, 30);

            foreach (var item in ApplesList)
            {
                Rect hitBox = new Rect(item.Left, item.Top, 30, 30);
                if (pacmanHitBox.IntersectsWith(hitBox))
                {
                    pacman.SetAlgorithm(new GiveSpeed());
                    pacman.Action(ref pacman);
                    var index = ApplesList.FindIndex(a => a.Top == item.Top && a.Left == item.Left);
                    Apples.RemoveAt(index);
                    ApplesList.RemoveAt(index);
                    break;
                }
            }

            foreach (var item in RottenApplesList)
            {
                Rect hitBox = new Rect(item.Left, item.Top, 30, 30);
                if (pacmanHitBox.IntersectsWith(hitBox))
                {
                    pacman.SetAlgorithm(new ReduceSpeed());
                    pacman.Action(ref pacman);
                    var index = RottenApplesList.FindIndex(a => a.Top == item.Top && a.Left == item.Left);
                    RottenApples.RemoveAt(index);
                    RottenApplesList.RemoveAt(index);
                    break;
                }
            }

        }

        public override void OnRightClick()
        {
            if (!noRight)
            {
                noLeft = noUp = noDown = false;
                goLeft = goUp = goDown = false;

                goRight = true;
            }
        }

        public override void OnDownClick()
        {
            if (!noDown)
            {
                noUp = noLeft = noRight = false;
                goUp = goLeft = goRight = false;

                goDown = true;
            }
        }

        public override void OnUpClick()
        {
            if (!noUp)
            {
                noRight = noDown = noLeft = false;
                goRight = goDown = goLeft = false;

                goUp = true;
            }
        }

        public override void OnLeftClick()
        {
            if (!noLeft)
            {
                goRight = goUp = goDown = false;
                noRight = noUp = noDown = false;

                goLeft = true;
            }
        }
    }
}