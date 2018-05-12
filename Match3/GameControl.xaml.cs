using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Match3
{
    /// <summary>
    /// Interaction logic for GameControl.xaml
    /// </summary>
    public partial class GameControl : Game.IGameWindow
    {

        private readonly Game _gameInstance;
        private readonly Register _removeAnimationRegister;
        private readonly Register _moveAnimationRegister;
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public GameControl()
        {
            InitializeComponent();
            _moveAnimationRegister = new Register();
            _moveAnimationRegister.Empty += OnMoveAnimationEmpty;
            _removeAnimationRegister = new Register();
            _removeAnimationRegister.Empty += OnRemoveAnimationEmpty;
            _gameInstance = new Game(this);
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTimerTick;          
        }

        private void OnTimerTick(object sender, EventArgs args)
        {
            var time = Convert.ToInt32(TimeLabel.Content) - 1;
            if (time == 0)
                ((ContentControl)Parent).Content = new GameOverControl();
            TimeLabel.Content = time.ToString();                
        }

        private void OnMoveAnimationEmpty(object sender, EventArgs args)
        {
            MoveDone?.Invoke(this, EventArgs.Empty);
            if (!_timer.IsEnabled) _timer.Start();
        }

        private void OnRemoveAnimationEmpty(object sender, EventArgs args)
        {
            RemoveDone?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler RemoveDone;
        public event EventHandler MoveDone;

        public Game.IGameTile CreateTile(Tuple<int, int> position, Game.TileType type)
        {
            var tile = new GameTile(type, position, _moveAnimationRegister);
            GameCanvas.Children.Add(tile);
            return tile;
        }

        public void RemoveTile(Game.IGameTile tile)
        {
            ScoreLabel.Content = (Convert.ToInt32(ScoreLabel.Content) + 1).ToString();
            var gTile = (GameTile)tile;
            var anim = new DoubleAnimation(0, TimeSpan.FromSeconds(0.2));
            anim.Completed += (sender, eArgs) =>
            {
                GameCanvas.Children.Remove(gTile);
                _removeAnimationRegister.UnregisterObject(gTile);
            };
            _removeAnimationRegister.RegisterObject(gTile);
            gTile.BeginAnimation(OpacityProperty, anim);
        }

        private void GameCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var dx = GameCanvas.ActualWidth / 8;
            var dy = GameCanvas.ActualHeight / 8;
            var p = e.GetPosition(GameCanvas);
            _gameInstance.Select(Tuple.Create((int)Math.Floor(p.Y / dy), (int)Math.Floor(p.X / dx)));
        }

        public Game.IGameTile CreateTileMove(Tuple<int, int> initPosition, Tuple<int, int> endPosition, Game.TileType type)
        {
            var tile = (GameTile)CreateTile(initPosition, type);
            tile.Loaded += (sender, eArgs) =>
            {
                tile.MoveTo(endPosition.Item2, endPosition.Item1);
            };
            return tile;
        }
    }

    public class GameTile : Shape, Game.IGameTile
    {
        private readonly Register _moveAnimationRegister;

        public GameTile(Game.TileType type, Tuple<int, int> pos, Register register)
        {
            Type = type;
            Position = pos;
            _moveAnimationRegister = register;
        }

        public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(
                "Type", typeof(Game.TileType), typeof(GameTile)
            );

        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
                "Position", typeof(Tuple<int, int>), typeof(GameTile)
            );

        public Game.TileType Type
        {
            get => (Game.TileType)GetValue(TypeProperty);
            set => SetValue(TypeProperty, value);
        }

        public Tuple<int, int> Position
        {
            get => (Tuple<int, int>)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        public static readonly Dictionary<Game.TileType, SolidColorBrush> TileTypeColor = 
            new Dictionary<Game.TileType, SolidColorBrush>
        {
            { Game.TileType.Blue, new SolidColorBrush(Colors.Blue) },
            { Game.TileType.Red, new SolidColorBrush(Colors.Red) },
            { Game.TileType.Green, new SolidColorBrush(Colors.Green) },
            { Game.TileType.Yellow, new SolidColorBrush(Colors.Yellow) },
            { Game.TileType.Purple, new SolidColorBrush(Colors.Purple) }
        };

        protected override Geometry DefiningGeometry =>
                new RectangleGeometry(
                    new Rect(ActualWidth * 0.15, ActualHeight * 0.15, ActualWidth * 0.7, ActualHeight * 0.7), 
                    ActualWidth * 0.2, ActualHeight * 0.2);

        public bool IsMoving => _moveAnimationRegister.IsRegistered(this);
        public bool ToBeDeleted { get; set; }

        public void Select()
        {
            DoubleAnimation anim = new DoubleAnimation(0.5, TimeSpan.FromSeconds(0.5))
            {
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true
            };
            BeginAnimation(OpacityProperty, anim);
        }

        public void Deselect()
        {
            BeginAnimation(OpacityProperty, null);            
        }

        private void AnimateMove(int x, int y, bool reverse)
        {
            var dx = x - Position.Item2;
            var dy = y - Position.Item1;
            DependencyProperty property;
            Double valueFrom, valueTo;
            TimeSpan duration;
            if (dx != 0)
            {
                property = Canvas.LeftProperty;
                valueFrom = Position.Item2 * ActualWidth;
                valueTo = valueFrom + dx * ActualWidth;
                duration = TimeSpan.FromSeconds(0.25 * Math.Abs(dx));
            }
            else
            {
                property = Canvas.TopProperty;
                valueFrom = Position.Item1 * ActualHeight;
                valueTo = valueFrom + dy * ActualHeight;
                duration = TimeSpan.FromSeconds(0.25 * Math.Abs(dy));
            }
            DoubleAnimation anim = new DoubleAnimation(valueFrom, valueTo, duration)
            {
                AutoReverse = reverse
            };
            if (reverse)
                anim.Completed += (sender, eArgs) => _moveAnimationRegister.UnregisterObject(this);
            else
                anim.Completed += (sender, eArgs) => {
                    Position = Tuple.Create(Position.Item1 + dy, Position.Item2 + dx);
                    _moveAnimationRegister.UnregisterObject(this);
                };
            _moveAnimationRegister.RegisterObject(this);
            BeginAnimation(property, anim);
        }

        public void MoveTo(int x, int y)
        {
            AnimateMove(x, y, false);
        }

        public void MoveError(int x, int y)
        {
            AnimateMove(x, y, true);
        }
    }

    public class TilePositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var d = (double)values[0];
            var position = (Tuple<int, int>)values[1];
            var isFirstItem = System.Convert.ToBoolean(parameter);
            if (isFirstItem)
                return position.Item1 * d;
            return position.Item2 * d;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TileFillColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? GameTile.TileTypeColor[(Game.TileType) value] : new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GridLineCoordConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double?) value * System.Convert.ToInt32(parameter) / 8 ?? 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TileDimConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double?) value / 8 ?? 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
