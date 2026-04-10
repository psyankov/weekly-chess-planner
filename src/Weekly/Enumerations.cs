namespace Ant.Weekly;

public enum GameResult
{
   None,
   WhiteWin,
   BlackWin,
   Draw
}

public enum PieceColor
{
   Any,
   White,
   Black
}

public enum ColorAssignment
{
   Requested,
   Fair,
   Random
}

public enum Section
{
   N = 0,
   E = 1,
   D = 2,
   C = 3,
   B = 4,
   A = 5,
}