using System;
using System.Collections.Generic;

namespace Match3
{
    public class Game
    {

        public enum TileType
        {
            Blue, Red, Green, Yellow, Purple
        }

        public interface IGameTile
        {
            void Select();
            void Deselect();
            void MoveTo(int x, int y);
            void MoveError(int x, int y);
            TileType Type { get; }
            bool IsMoving { get; } 
            bool ToBeDeleted { get; set; }
        }

        public interface IGameWindow
        {
            IGameTile CreateTileMove(Tuple<int, int> initPosition, Tuple<int, int> endPosition, TileType type);
            void RemoveTile(IGameTile tile);
            event EventHandler RemoveDone;
            event EventHandler MoveDone;
        }

        private static readonly Array TileTypeValues = Enum.GetValues(typeof(TileType));
        
        private readonly IGameTile[,] _tiles = new IGameTile[8, 8];
        private readonly Random _tileTypeRandom = new Random();
        private readonly IGameWindow _window;
        private static readonly Tuple<int, int> NullTileSelection = Tuple.Create(-1, -1);
        private Tuple<int, int> _selectedTile = NullTileSelection;
        private readonly object _tilesLock = new object();
        private bool _canSelect;

        public Game(IGameWindow window)
        {
            _window = window;
            window.MoveDone += OnMoveDone;
            window.RemoveDone += OnRemoveDone;
            for (int i = 0; i < _tiles.GetLength(0); ++i)
                for (int j = 0; j < _tiles.GetLength(1); ++j)
                    _tiles[i, j] = _window.CreateTileMove(Tuple.Create(i - 8, j), Tuple.Create(i, j),
                        (TileType)TileTypeValues.GetValue(_tileTypeRandom.Next(TileTypeValues.Length)));
        }

        private void SwapTiles(Tuple<int, int> pos1, Tuple<int, int> pos2)
        {
            var temp = Utils.Get(_tiles, pos1);
            Utils.Set(_tiles, pos1, Utils.Get(_tiles, pos2));
            Utils.Set(_tiles, pos2, temp);
        }

        public void Select(Tuple<int, int> pos)
        {
            var pGameTile = Utils.Get(_tiles, pos);
            if (pGameTile == null || pGameTile.IsMoving || !_canSelect) return;
            if (Equals(_selectedTile, NullTileSelection))
                pGameTile.Select();
            else
            {
                var sGameTile = Utils.Get(_tiles, _selectedTile);
                if (!Equals(_selectedTile, pos))
                {
                    var dy = _selectedTile.Item1 - pos.Item1;
                    var dx = _selectedTile.Item2 - pos.Item2;
                    if (Math.Abs(dx) + Math.Abs(dy) == 1)
                    {
                        SwapTiles(_selectedTile, pos);
                        if (Check(_selectedTile, pos))
                        {
                            sGameTile.MoveTo(pos.Item2, pos.Item1);
                            pGameTile.MoveTo(_selectedTile.Item2, _selectedTile.Item1);
                        }
                        else
                        {
                            SwapTiles(_selectedTile, pos);
                            sGameTile.MoveError(pos.Item2, pos.Item1);
                            pGameTile.MoveError(_selectedTile.Item2, _selectedTile.Item1);
                        }
                        pos = NullTileSelection;
                    }
                    else
                        pGameTile.Select();
                }
                else
                    pos = NullTileSelection;
                sGameTile.Deselect();
            }
            _selectedTile = pos;
        }

        private static readonly Tuple<int, int>[] MoveIndex = {
            Tuple.Create(0, -1),
            Tuple.Create(-1, 0),
            Tuple.Create(0, 1),
            Tuple.Create(1, 0)
        };

        private bool Check(Tuple<int, int> pos1, Tuple<int, int> pos2)
        {
            return Check(pos1) || Check(pos2);
        }

        private bool Check(Tuple<int, int> pos)
        {
            var type = Utils.Get(_tiles, pos).Type;
            return CountSame(Utils.Add(pos, MoveIndex[0]), MoveIndex[0], type) +
                CountSame(Utils.Add(pos, MoveIndex[2]), MoveIndex[2], type) + 1 >= 3 ||
                CountSame(Utils.Add(pos, MoveIndex[1]), MoveIndex[1], type) +
                CountSame(Utils.Add(pos, MoveIndex[3]), MoveIndex[3], type) + 1 >= 3;

        }

        private int CountSame(Tuple<int, int> pos, Tuple<int, int> dir, TileType type)
        {
            var count = 0;
            while (pos.Item1 < 8 && pos.Item1 >= 0 && pos.Item2 < 8 && pos.Item2 >= 0)
            {
                if (Utils.Get(_tiles, pos).Type != type)
                    break;
                count += 1;
                pos = Utils.Add(pos, dir);
            }
            return count;
        }

        private List<IGameTile> GetSame(Tuple<int, int> pos, Tuple<int, int> dir, TileType type)
        {
            var result = new List<IGameTile>();
            while (pos.Item1 < 8 && pos.Item1 >= 0 && pos.Item2 < 8 && pos.Item2 >= 0)
            {
                var tile = Utils.Get(_tiles, pos);
                if (tile.Type != type)
                    break;
                result.Add(tile);
                pos = Utils.Add(pos, dir);
            }
            return result;
        }

        private static void MarkToBeDeleted(IEnumerable<IGameTile> list)
        {
            foreach (var element in list)
                element.ToBeDeleted = true;
        }

        private bool CheckMatch(Tuple<int, int> pos)
        {
            var tile = Utils.Get(_tiles, pos);
            var type = tile.Type;
            var left = GetSame(Utils.Add(pos, MoveIndex[0]), MoveIndex[0], type);
            var up = GetSame(Utils.Add(pos, MoveIndex[1]), MoveIndex[1], type);
            var right = GetSame(Utils.Add(pos, MoveIndex[2]), MoveIndex[2], type);
            var down = GetSame(Utils.Add(pos, MoveIndex[3]), MoveIndex[3], type);
            var row = left.Count + right.Count + 1 >= 3;
            var column = up.Count + down.Count + 1 >= 3;
            if (!row && !column)
                return false;
            tile.ToBeDeleted = true;
            if (row)
            {
                MarkToBeDeleted(left);
                MarkToBeDeleted(right);
            }

            if (!column) return true;
            MarkToBeDeleted(down);
            MarkToBeDeleted(up);
            return true;
        }

        private void OnMoveDone(object sender, EventArgs args)
        {
            lock (_tilesLock)
            {
                _canSelect = false;
                bool found = false;
                for (int i = 0; i < 8; ++i)
                    for (int j = 0; j < 8; ++j)
                        found = CheckMatch(Tuple.Create(i, j)) || found;
                if (found)
                {
                    for (int i = 0; i < 8; ++i)
                        for (int j = 0; j < 8; ++j)
                            if (_tiles[i, j].ToBeDeleted)
                            {
                                _window.RemoveTile(_tiles[i, j]);
                                _tiles[i, j] = null;
                            }
                }
                else
                    _canSelect = true;
            }
        }

        private void OnRemoveDone(object sender, EventArgs agrs)
        {
            lock (_tilesLock)
            {
                for (var j = 0; j < 8; ++j)
                {
                    var count = 0;
                    for (var i = 7; i >= 0; --i)
                        if (_tiles[i, j] == null)
                            for (var k = i - 1; k >= 0; --k)
                                if (_tiles[k, j] != null)
                                {
                                    SwapTiles(Tuple.Create(i, j), Tuple.Create(k, j));
                                    _tiles[i, j]?.MoveTo(j, i);
                                    break;
                                }
                    for (var i = 7; i >= 0; --i)
                        if (_tiles[i, j] == null)
                        {
                            _tiles[i, j] = _window.CreateTileMove(Tuple.Create(count - 1, j), Tuple.Create(i, j),
                                (TileType)TileTypeValues.GetValue(_tileTypeRandom.Next(TileTypeValues.Length)));
                            count -= 1;
                        }
                }
            }
        }
    }

}
