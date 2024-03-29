﻿using Domain.GamblingMachines.Abstractions;

namespace Domain.GamblingMachines.VegetableFiesta;

public class VegetableFiestaSlotMachine : AbstractSlotMachine<VegetableFiestaResult>
{
    //10x10 grid
    private const int ColumnSize = 10;
    private const int MinWinCount = 6;
    private static readonly VegetableFiestaTileSet[] Tiles = Enum.GetValues<VegetableFiestaTileSet>()
        .Where(x => x != VegetableFiestaTileSet.Empty)
        .ToArray();

    
    public override VegetableFiestaResult Spin()
    {
        var board = GamblingMachineTools.GenerateInitialBoard(Tiles, ColumnSize*ColumnSize);
        var result = new VegetableFiestaResult(GamblingMachineTools.Transform2d(board, ColumnSize, ColumnSize));

        while (true)
        {
            var winningIndex = FindMatches(board);
            HandleMultiplier(winningIndex, result, board);
            if (winningIndex.Count == 0) break;
            ProgressBoard(board, winningIndex);
        }

        return result;
    }

    private static void ProgressBoard(VegetableFiestaTileSet[] board, ICollection<ISet<int>> winningIndex)
    {
        foreach (var index in winningIndex.SelectMany(x => x))
        {
            board[index] = VegetableFiestaTileSet.Empty;
        }
        
        for (int i = board.Length - 1; i >= ColumnSize; i--)
        {
            if (board[i] != VegetableFiestaTileSet.Empty) continue;
            var swindex = i;
            for (int j = i-ColumnSize; j >= 0; j-=ColumnSize)
            {
                if (board[j] == VegetableFiestaTileSet.Empty) continue;
                swindex = j;
                break;
            }
            if (swindex == i) continue;
            board[i] = board[swindex];
            board[swindex] = VegetableFiestaTileSet.Empty;
        }

        for (int i = 0; i < ColumnSize*ColumnSize; i++)
        {
            if (board[i] != VegetableFiestaTileSet.Empty) continue;
            board[i] = Tiles[GamblingMachineTools.Rand.Next(0, Tiles.Length)];
        }
    }
    
    private static void HandleMultiplier(ICollection<ISet<int>> winningIndex, VegetableFiestaResult result,
        VegetableFiestaTileSet[] board)
    {
        var multipliers = new List<float>();
        foreach (var winSet in winningIndex)
        {
            var symbol = winSet
                .Select(x => board[x])
                .FirstOrDefault(x => x != VegetableFiestaTileSet.Wild, VegetableFiestaTileSet.Wild);
            multipliers.Add(CalcMultiplier(winSet.Count, symbol));
        }
        result.AddState(new VegetableFiestaState(
            GamblingMachineTools.Transform2d(board, ColumnSize, ColumnSize), 
            multipliers.Where(x => x > 0).ToArray())
        );
    }

    private static float CalcMultiplier(int count, VegetableFiestaTileSet symbol)
    {
        if (count < MinWinCount) return -1f;
        var localCount = (int)Math.Pow(count - MinWinCount + 1, 2);
        return (symbol switch
        {
            VegetableFiestaTileSet.Empty => -1f,
            VegetableFiestaTileSet.Scatter => 6f,
            VegetableFiestaTileSet.Star => 0.5f,
            VegetableFiestaTileSet.Heart => 0.4f,
            VegetableFiestaTileSet.Carrot => 0.6f,
            VegetableFiestaTileSet.Lettuce => 0.8f,
            VegetableFiestaTileSet.Cucumber => 1f,
            VegetableFiestaTileSet.Beets => 1.5f,
            VegetableFiestaTileSet.Tomato => 2f,
            VegetableFiestaTileSet.Wild => -1f,
            _ => throw new ArgumentOutOfRangeException(nameof(symbol), symbol, null)
        }) * localCount;
    }

    private static ICollection<ISet<int>> FindMatches(VegetableFiestaTileSet[] board)
    {
        var globalVisited = new HashSet<int>();
        var localVisited = new HashSet<int>();
        var winningIndexes = new List<ISet<int>>();
        for (int i = 0; i < board.Length; i++)
        {
            if(board[i] == VegetableFiestaTileSet.Wild || board[i] == VegetableFiestaTileSet.Scatter) continue;
            
            localVisited.Clear();
            VisitIndex(i, board[i], board, localVisited, globalVisited);
            if(localVisited.Count < MinWinCount) continue;
            
            globalVisited.UnionWith(localVisited.Where(x => board[x] != VegetableFiestaTileSet.Wild));
            winningIndexes.Add(new HashSet<int>(localVisited));
        }

        return winningIndexes;
    }

    private static void VisitIndex(int index, VegetableFiestaTileSet symbol, VegetableFiestaTileSet[] board, ISet<int> local, IReadOnlySet<int> global)
    {
        if(local.Contains(index) || global.Contains(index)) return;
        if(board[index] != symbol && board[index] != VegetableFiestaTileSet.Wild) return;
        local.Add(index);
        if(index >= ColumnSize) 
            VisitIndex(index - ColumnSize, symbol, board, local, global); //go up
        if(index < ((ColumnSize*ColumnSize)-ColumnSize)-1) 
            VisitIndex(index + ColumnSize, symbol, board, local, global); //go down
        if(index % ColumnSize != 0) 
            VisitIndex(index - 1, symbol, board, local, global); //go left
        if(index % ColumnSize != ColumnSize-1) 
            VisitIndex(index + 1, symbol, board, local, global); //go right

    }
}