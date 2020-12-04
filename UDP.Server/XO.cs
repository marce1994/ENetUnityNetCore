using System;
using UDP.Core.Model.Packet.Enum;

namespace UDP.Server
{
    public static class XO
    {
        public static int Evaluate(ref EValue[] board, Tuple<uint, EValue> move)
        {
            if (board[move.Item1] != EValue.EMPTY)
                return -1;
            else
                board[move.Item1] = move.Item2;

            var result = Heuristic(board);

            if (result > 0)
                return (int)EValue.X;
            else if (result < 0)
                return (int)EValue.O;
            else
                return 0;
        }

        private static int Heuristic(EValue[] board)
        {
            EValue[,] _board = new EValue[,]
            {
                { board[0], board[1], board[2] },
                { board[3], board[4], board[5] },
                { board[6], board[7], board[8] },
            };

            for (int i = 0; i < 3; i++)
            {
                if (_board[i, 0] == _board[i, 1] && _board[i, 1] == _board[i, 2])
                {
                    if (_board[i, 0] == EValue.O)
                        return 10;
                    else if (_board[i, 0] == EValue.X)
                        return -10;
                }
            }

            for (int j = 0; j < 3; j++)
            {
                if (_board[0, j] == _board[1, j] &&
                    _board[1, j] == _board[2, j])
                {
                    if (_board[0, j] == EValue.O)
                        return 10;
                    else if (_board[0, j] == EValue.X)
                        return -10;
                }
            }

            if (_board[0, 0] == _board[1, 1] && _board[1, 1] == _board[2, 2])
            {
                if (_board[0, 0] == EValue.O)
                {
                    return 10;
                }
                else if (_board[0, 0] == EValue.X)
                    return -10;
            }

            if (_board[0, 2] == _board[1, 1] && _board[1, 1] == _board[2, 0])
            {
                if (_board[0, 2] == EValue.O)
                {
                    return 10;
                }
                else if (_board[0, 2] == EValue.X)
                    return -10;
            }

            return 0;
        }
    }
}
