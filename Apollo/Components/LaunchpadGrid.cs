﻿using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Input;

namespace Apollo.Components {
    public class LaunchpadGrid: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        UniformGrid Grid;
        Path TopLeft, TopRight, BottomLeft, BottomRight;
        Shape ModeLight;

        public delegate void PadChangedEventHandler(int index);
        public event PadChangedEventHandler PadPressed;
        public event PadChangedEventHandler PadReleased;

        public static int GridToSignal(int index) => (index == -1)? 99 : ((9 - (index / 10)) * 10 + index % 10);
        public static int SignalToGrid(int index) => (index == 99)? -1 : ((9 - (index / 10)) * 10 + index % 10);

        public void SetColor(int index, SolidColorBrush color) {
            if (index == 0 || index == 9 || index == 90 || index == 99) return;

            if (index == -1) ModeLight.Fill = color;
            else ((Shape)Grid.Children[index]).Fill = color;
        }

        private double _scale = 1;
        public double Scale {
            get => _scale;
            set {
                value = Math.Max(0, value);
                if (value != _scale) {
                    _scale = value;

                    this.Resources["PadSquareSize"] = 15 * Scale;
                    this.Resources["PadCircleSize"] = 11 * Scale;
                    this.Resources["PadCut1"] = 3 * Scale;
                    this.Resources["PadCut2"] = 12 * Scale;
                    this.Resources["ModeWidth"] = 4 * Scale;
                    this.Resources["ModeHeight"] = 2 * Scale;
                    this.Resources["PadMargin"] = new Thickness(1 * Scale);
                    this.Resources["ModeMargin"] = new Thickness(0, 5 * Scale, 0, 0);
                    
                    DrawPath();
                }
            }
        }

        public string FormatPath(string format) => String.Format(format,
            this.Resources["PadSquareSize"],
            this.Resources["PadCut1"],
            this.Resources["PadCut2"]
        );

        public void DrawPath() {
            TopLeft.Data = Geometry.Parse(FormatPath("M 0,0 L 0,{0} {2},{0} {0},{2} {0},0 Z"));
            TopRight.Data = Geometry.Parse(FormatPath("M 0,0 L 0,{2} {1},{0} {0},{0} {0},0 Z"));
            BottomLeft.Data = Geometry.Parse(FormatPath("M 0,0 L 0,{0} {0},{0} {0},{1} {2},0 Z"));
            BottomRight.Data = Geometry.Parse(FormatPath("M 0,{1} L 0,{0} {0},{0} {0},0 {1},0 Z"));
        }

        public LaunchpadGrid() {
            InitializeComponent();

            Grid = this.Get<UniformGrid>("LaunchpadGrid");

            TopLeft = this.Get<Path>("TopLeft");
            TopRight = this.Get<Path>("TopRight");
            BottomLeft = this.Get<Path>("BottomLeft");
            BottomRight = this.Get<Path>("BottomRight");

            ModeLight = this.Get<Rectangle>("ModeLight");
        }

        private void LayoutChanged(object sender, EventArgs e) => DrawPath();

        bool mouseHeld = false;

        private void MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                mouseHeld = true;
                MouseEnter(sender, new PointerEventArgs());
            }
        }

        private void MouseUp(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                MouseLeave(sender, new PointerEventArgs());
                mouseHeld = false;
            }
        }

        private void MouseEnter(object sender, PointerEventArgs e) {
            if (mouseHeld) PadPressed?.Invoke(Grid.Children.IndexOf((IControl)sender));
        }

        private void MouseLeave(object sender, PointerEventArgs e) {
            if (mouseHeld) PadReleased?.Invoke(Grid.Children.IndexOf((IControl)sender));
        }
    }
}
