﻿using ClassLibrary._Pacman;
using ClassLibrary.Coins.Factories;
using ClassLibrary.Coins.Interfaces;
using ClassLibrary.Commands;
using ClassLibrary.Decorator;
using ClassLibrary.Fruits;
using ClassLibrary.Mobs;
using ClassLibrary.Mobs.StrongMob;
using ClassLibrary.Mobs.WeakMob;
using ClassLibrary.Strategies;
using ClassLibrary.Views;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WPF.Connection;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace WPF.Game.ViewModels
{
    public class ThirdLevelViewModel : LevelViewModelBase
    {
        DispatcherTimer gameTimer = new DispatcherTimer();
        bool goLeft, goRight, goUp, goDown;
        bool noLeft, noRight, noUp, noDown;
        CoinFactory _coinFactory;
        HubConnection _connection;
        WeakMobFactory _mobFactory;
        StrongMobFactory _strongMobFactory;
        Pacman pacman;
        Pacman greenPacman;
        Grid mainGrid;
        Grid opponentGrid;
        public Canvas LayoutRoot { get; private set; }
        public int YellowLeft
        {
            get
            {
                return pacman.Left;
            }
            private set
            {
                if (value != pacman.Left)
                {
                    pacman.Left = value;
                    OnPropertyChanged("YellowLeft");
                }
            }
        }
        public int YellowTop
        {
            get
            {
                return pacman.Top;
            }
            private set
            {
                if (value != pacman.Top)
                {
                    pacman.Top = value;
                    OnPropertyChanged("YellowTop");
                }
            }
        }
        public int GreenLeft
        {
            get
            {
                return greenPacman.Left;
            }
            private set
            {
                if (value != greenPacman.Left)
                {
                    greenPacman.Left = value;
                    OnPropertyChanged("GreenLeft");
                }
            }
        }
        public int GreenTop
        {
            get
            {
                return greenPacman.Top;
            }
            private set
            {
                if (value != greenPacman.Top)
                {
                    greenPacman.Top = value;
                    OnPropertyChanged("GreenTop");
                }
            }
        }

        public ObservableCollection<Coin> Coins { get; set; }
        public ObservableCollection<Mob> Mobs { get; set; }
        public ObservableCollection<Apple> Apples { get; set; }
        public ObservableCollection<RottenApple> RottenApples { get; set; }
        public ObservableCollection<Cherry> Cherries { get; set; }
        public ObservableCollection<Strawberry> Strawberries { get; set; }

        PacmanHitbox myPacmanHitBox = PacmanHitbox.GetInstance;
        public int score
        {
            get
            {
                return pacman.Score;
            }
            private set
            {
                pacman.Score = value;
                OnPropertyChanged("score");
            }
        }

        public int opponentScore
        {
            get
            {
                return greenPacman.Score;
            }
            private set
            {
                greenPacman.Score = value;
                OnPropertyChanged("opponentScore");
            }
        }
        public ThirdLevelViewModel(IConnectionProvider connectionProvider)
        {
            _coinFactory = new SilverCoinCreator();
            _mobFactory = new WeakMobFactory();
            _strongMobFactory = new StrongMobFactory();
            _connection = connectionProvider.GetConnection();
            pacman = new Pacman("Pacman");
            greenPacman = pacman.Copy();
            LayoutRoot = new Canvas();
            LayoutRoot.Name = "MyCanvas";
            IDecorator grid = new AddLabel(new AddHealthBar(pacman, 100));
            mainGrid = grid.Draw();
            opponentGrid = new AddLabel(new AddHealthBar(greenPacman, 100)).Draw();
            LayoutRoot.Children.Add(mainGrid);
            LayoutRoot.Children.Add(opponentGrid);
            GreenTop = 20;
            GreenLeft = 20;
            YellowLeft = 20;
            YellowTop = 20;

            Coins = Utils.Utils.GetCoins(_coinFactory);
            Mobs = SpawnMobs();
            Apples = Utils.Utils.CreateApples();
            RottenApples = Utils.Utils.CreateRottenApples();
            Cherries = Utils.Utils.CreateCherries();
            Strawberries = Utils.Utils.CreateStrawberries();
            ListenServer();
            GameSetup();
        }
        private ObservableCollection<Mob> SpawnMobs()
        {
            ObservableCollection<Mob> result = new ObservableCollection<Mob>();
            var firstGhost = _strongMobFactory.CreateGhost(500, 600);
            var secondGhost = _strongMobFactory.CreateGhost(50, 750);
            var firstZombie = _mobFactory.CreateZombie(500, 50);
            var secondZombie = _mobFactory.CreateZombie(300, 300);
            result.Add(firstGhost);
            result.Add(secondGhost);
            result.Add(firstZombie);
            result.Add(secondZombie);
            return result;
        }

        private void ListenServer()
        {
            _connection.On<string>("OponentCordinates", (serializedObject) =>
            {
                Pacman deserializedObject = JsonSerializer.Deserialize<Pacman>(serializedObject);
                GreenLeft = deserializedObject.Left;
                GreenTop = deserializedObject.Top;
            });

            _connection.On<RemoveAppleAtIndexCommand>("RemoveAppleAtIndex", (command) =>
            {
                command.Execute(Apples);
            });

            _connection.On<RemoveRottenAppleAtIndexCommand>("RemoveRottenAppleAtIndex", (command) =>
            {
                command.Execute(RottenApples);
            });

            _connection.On<RemoveCoinAtIndexCommand>("RemoveCoinAtIndex", async (command) =>
            {
                command.Execute(Coins);
                if (Coins.Count == 0)
                {
                    gameTimer.Stop();
                    await _connection.InvokeAsync("LevelUp", 4);
                }
            });

            _connection.On<RemoveCherryAtIndexCommand>("RemoveCherryAtIndex", (command) =>
            {
                command.Execute(Cherries);
            });
            _connection.On<GivePointsToOpponentCommand>("OpponentScore", (command) =>
            {
                command.Execute(greenPacman);
                opponentScore = greenPacman.Score;
            });
            _connection.On<string>("Move", (pos) =>
            {
                var deserializedObject = JsonConvert.DeserializeObject<dynamic>(pos);
                Mobs[(int)deserializedObject.Index].GoLeft = (bool)deserializedObject.GoLeft;
                Mobs[(int)deserializedObject.Index].Left = (int)deserializedObject.Position;
            });
            _connection.On<int>("PacmanDamage", (damage) =>
            {
                IDecorator grid = new AddLabel(new AddHealthBar(greenPacman, damage));
                opponentGrid = grid.Draw();
                LayoutRoot.Children.Remove(LayoutRoot.Children[1]);
                LayoutRoot.Children.Insert(1, opponentGrid);
                Canvas.SetLeft(opponentGrid, GreenLeft);
                Canvas.SetTop(opponentGrid, GreenTop);
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
            Canvas.SetLeft(mainGrid, YellowLeft);
            Canvas.SetTop(mainGrid, YellowTop);
            Canvas.SetLeft(opponentGrid, GreenLeft);
            Canvas.SetTop(opponentGrid, GreenTop);

            int AppHeight = (int)Application.Current.MainWindow.Height;
            int AppWidth = (int)Application.Current.MainWindow.Width;
            int oldLeft = YellowLeft;
            int oldTop = YellowTop;
            if (goRight)
            {
                YellowLeft += pacman.Speed;
            }
            if (goLeft)
            {
                YellowLeft -= pacman.Speed;
            }
            if (goUp)
            {
                YellowTop -= pacman.Speed;
            }
            if (goDown)
            {
                YellowTop += pacman.Speed;
            }

            if (oldLeft != YellowLeft || oldTop != YellowTop)
            {
                string serializedObject = JsonSerializer.Serialize(new { Top = pacman.Top, Left = pacman.Left });
                await _connection.InvokeAsync("SendPacManCordinates", serializedObject);
            }

            if (goDown && YellowTop + 280 > AppHeight)
            {
                noDown = true;
                goDown = false;
            }
            if (goUp && YellowTop < 5)
            {
                noUp = true;
                goUp = false;
            }
            if (goLeft && YellowLeft - 5 < 1)
            {
                noLeft = true;
                goLeft = false;
            }
            if (goRight && YellowLeft + 40 > AppWidth)
            {
                noRight = true;
                goRight = false;
            }

            Rect pacmanHitBox = myPacmanHitBox.GetCurrentHitboxPosition(YellowLeft, YellowTop, 30, 30);

            foreach (var item in Apples)
            {
                Rect hitBox = new Rect(item.Left, item.Top, 30, 30);
                if (pacmanHitBox.IntersectsWith(hitBox))
                {
                    pacman.SetAlgorithm(new GiveSpeed());
                    pacman.Action(ref pacman);
                    var index = Apples.IndexOf(Apples.Where(a => a.Top == item.Top && a.Left == item.Left).FirstOrDefault());
                    Apples.RemoveAt(index);
                    break;
                }
            }

            foreach (var item in RottenApples)
            {
                Rect hitBox = new Rect(item.Left, item.Top, 30, 30);
                if (pacmanHitBox.IntersectsWith(hitBox))
                {
                    pacman.SetAlgorithm(new ReduceSpeed());
                    pacman.Action(ref pacman);
                    var index = RottenApples.IndexOf(RottenApples.Where(a => a.Top == item.Top && a.Left == item.Left).FirstOrDefault());
                    RottenApples.RemoveAt(index);
                    break;
                }
            }

            foreach (var item in Coins)
            {
                Rect hitBox = new Rect(item.Left, item.Top, 10, 10);
                if (pacmanHitBox.IntersectsWith(hitBox))
                {
                    var index = Coins.IndexOf(Coins.Where(a => a.Top == item.Top && a.Left == item.Left).FirstOrDefault());
                    if (index != -1)
                    {
                        await _connection.InvokeAsync("SendRemoveCoinAtIndex", new RemoveCoinAtIndexCommand(index));
                        Coins.RemoveAt(index);
                        pacman.Score += item.Value;
                        score = pacman.Score;
                        await _connection.InvokeAsync("GivePointsToOpponent", new GivePointsToOpponentCommand(score));
                    }
                   
                    break;
                }
            }

            foreach (var item in Cherries)
            {
                Rect hitBox = new Rect(item.Left, item.Top, 30, 30);
                if (pacmanHitBox.IntersectsWith(hitBox))
                {
                    pacman.SetAlgorithm(new DoublePoints());
                    pacman.Action(ref pacman);
                    score = pacman.Score;
                    await _connection.InvokeAsync("GivePointsToOpponent", new GivePointsToOpponentCommand(score));
                    var index = Cherries.IndexOf(Cherries.Where(a => a.Top == item.Top && a.Left == item.Left).FirstOrDefault());
                    if(index != -1)
                    {
                        await _connection.InvokeAsync("SendRemoveCherryAtIndex", new RemoveCherryAtIndexCommand(index));
                        Cherries.RemoveAt(index);
                    }
                    break;
                }
            }

            int mobIndex = 0;
            foreach (var item in Mobs)
            {
                Rect hitBox = new Rect(item.Left, item.Top, 30, 30);
                if (pacmanHitBox.IntersectsWith(hitBox))
                {
                    pacman.Health -= item.GetDamage();
                    await _connection.InvokeAsync("PacmanDamage", pacman.Health);
                    IDecorator grid = new AddLabel(new AddHealthBar(pacman, pacman.Health));
                    mainGrid = grid.Draw();
                    LayoutRoot.Children.Remove(LayoutRoot.Children[0]);
                    LayoutRoot.Children.Insert(0, mainGrid);
                    Canvas.SetLeft(mainGrid, YellowLeft);
                    Canvas.SetTop(mainGrid, YellowTop);
                }
                if (_connection.State.HasFlag(HubConnectionState.Connected))
                {
                    if (item.GoLeft && item.Left + 40 > AppWidth)
                    {
                        string a = JsonConvert.SerializeObject(new { Position = item.Left, Index = mobIndex, GoLeft = false });
                        await _connection.InvokeAsync("Move", a);
                    }
                    else if (!item.GoLeft && item.Left - 5 < 1)
                    {
                        string a = JsonConvert.SerializeObject(new { Position = item.Left, Index = mobIndex, GoLeft = true });
                        await _connection.InvokeAsync("Move", a);
                    }
                    if (item.GoLeft)
                    {
                        string a = JsonConvert.SerializeObject(new { Position = item.Left + item.GetSpeed(), Index = mobIndex, GoLeft = true });
                        await _connection.InvokeAsync("Move", a);
                    }
                    else
                    {
                        string a = JsonConvert.SerializeObject(new { Position = item.Left - item.GetSpeed(), Index = mobIndex, GoLeft = false });
                        await _connection.InvokeAsync("Move", a);
                    }
                    mobIndex++;
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
