﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPF.Game.Classes
{
    public class Strawberry
    {
        public int Top { get; set; }
        public int Left { get; set; }
        public ImageBrush Fill { get; set; }

        public Strawberry(int top, int left)
        {
            Top = top;
            Left = left;
            ImageBrush strawberry = new ImageBrush();
            strawberry.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/strawberry.png"));
            Fill = strawberry;
        }
    }
}
