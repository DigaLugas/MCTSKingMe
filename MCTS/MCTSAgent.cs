using MCTS;
using System;
using System.Collections.Generic;

namespace MCTS    
{
    public class MCTSAgent
    {
        private readonly MCTSParallel _mcts;
        private readonly GameRules _rules;
        private readonly int _playerId;

        public MCTSAgent(int playerId, int maxIterations = 10000, int numThreads = 4)
        {
            _rules = new GameRules();
            _mcts = new MCTSParallel(_rules, maxIterations, 1.414, numThreads);
            _playerId = playerId;
        }

        public GameMove MakeMove(GameState state)
        {
            var bestMove = _mcts.FindBestMove(state, _playerId);
            if (bestMove == null)
            {
                // Se não houver movimento recomendado pelo MCTS, escolha aleatoriamente entre os movimentos válidos
                var validMoves = _rules.GetValidMoves(state);
                if (validMoves.Count > 0)
                {
                    return validMoves[new Random().Next(validMoves.Count)];
                }
                return null;
            }
            return bestMove;
        }

        public void ApplyMove(GameState state, GameMove move)
        {
            _rules.ApplyMove(state, move);
        }

        public List<GameMove> GetValidMoves(GameState state)
        {
            return _rules.GetValidMoves(state);
        }
    }
}