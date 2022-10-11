﻿using System.Windows.Media;

namespace ClassLibrary.Mobs.Interfaces
{
    public interface IGhost
    {
        public int Speed { get; set; }
        public int Top { get; set; }
        public int Left { get; set; }
        public ImageBrush Fill { get; set; }
    }
}