using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot {

    static IDictionary<PieceType, int> piecesValue = new Dictionary<PieceType, int>()
    {
        {PieceType.King, 10},
        {PieceType.Queen, 9},
        {PieceType.Rook, 5},
        {PieceType.Bishop, 3},
        {PieceType.Knight, 3},
        {PieceType.Pawn, 1},
        {PieceType.None, 0},
    };

    public Move Think(Board board, Timer timer)
    {
        bool white = board.IsWhiteToMove;
        Move move = BestMove(board, white, 3);
        Console.WriteLine(AmountCapturable(board, white));
        return move;
    }

    static Move BestMove(Board board, bool white, int depth)
    {
        Move bestMove = Move.NullMove;
        float highestEval = 0;
        foreach (Move move in board.GetLegalMoves())
        {
            float moveEval = EvalMove(board, move, white, depth);
            if (moveEval > highestEval)
            {
                bestMove = move;
                highestEval = moveEval;
            }
        }

        return bestMove;
    }

    static float EvalMove(Board board, Move firstMove, bool white, int depth)
    {
        board.MakeMove(firstMove);
        float finalEval = 0;
        if (depth-- > 1)
        {
            Move bestOpponentMove = BestMove(board, !white, depth);
            if (!bestOpponentMove.Equals(Move.NullMove))
            {
                board.MakeMove(bestOpponentMove);
                foreach (Move move in board.GetLegalMoves())
                {
                    float eval = EvalMove(board, move, white, depth);
                    if (eval > finalEval)
                    {
                        finalEval = eval;
                    }
                }
                board.UndoMove(bestOpponentMove);
            }
            else
            {
                finalEval = EvalPosition(board, white);
            }
        } 
        else
        {
            finalEval = EvalPosition(board, white);
        }
        board.UndoMove(firstMove);
        return finalEval;
    }

    static float EvalPosition(Board board, bool white)
    {
        if (board.IsInCheckmate())
        {
            return 100;
        }
        if (board.IsDraw())
        {
            return 0.5f;
        }

        int capturedPieceValue = AmountCapturable(board, !white);

        float boardValueWhite = 0;
        float boardValueBlack = 0;
        foreach (PieceType pieceType in piecesValue.Keys) {
            boardValueWhite += BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(pieceType, true)) * piecesValue[pieceType];
            boardValueBlack += BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(pieceType, false)) * piecesValue[pieceType];
        }

        if (white)
        {
            return (boardValueWhite - capturedPieceValue) / boardValueBlack;
        }
        return (boardValueBlack - capturedPieceValue) / boardValueWhite;
    }

    static Int32 AmountCapturable(Board board, Boolean white)
    {
        ulong opponentBitboard = white ? board.BlackPiecesBitboard : board.WhitePiecesBitboard;

        List<ulong> attackBoards = board.GetPieceList(PieceType.Pawn, white).Select(p => BitboardHelper.GetPawnAttacks(p.Square, white))
            .Concat(board.GetPieceList(PieceType.Rook, white)
                .Concat(board.GetPieceList(PieceType.Bishop, white))
                .Concat(board.GetPieceList(PieceType.Queen, white))
                .Select(p => BitboardHelper.GetSliderAttacks(p.PieceType, p.Square, board)))
            .Concat(board.GetPieceList(PieceType.Knight, white).Select(p => BitboardHelper.GetKnightAttacks(p.Square)))
            .ToList();

        attackBoards.Add(BitboardHelper.GetKingAttacks(board.GetKingSquare(white)));

        ulong attackable = attackBoards.Aggregate((x, y) => x | y);

        Int32 totalValue = 0;
        foreach (PieceType type in new PieceType[] {PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Bishop, PieceType.Rook, PieceType.Queen, PieceType.King})
        {
            totalValue += BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(type, !white) & attackable) * piecesValue[type];
        }
        return totalValue;
    }

    static void PrintBitBoard(ulong bitboard)
    {
        char[,] chessboard = new char[8, 8];

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                int index = row * 8 + col;
                ulong mask = 1UL << index;
                bool isSet = (bitboard & mask) != 0;

                if (isSet)
                {
                    // You can set the corresponding piece character based on the bitboard position
                    // For example, 'P' for pawn, 'R' for rook, 'N' for knight, etc.
                    chessboard[row, col] = 'X'; // Replace 'X' with the appropriate piece character
                }
                else
                {
                    chessboard[row, col] = '.';
                }
            }
        }

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Console.Write(chessboard[row, col] + " ");
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }
}