using MCTS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MCTS

{
    public class MCTSNode
    {
        public GameState State { get; }
        public GameMove Move { get; }
        public MCTSNode Parent { get; }
        public List<MCTSNode> Children { get; }
        public int Visits { get; private set; }
        public double TotalScore { get; private set; }
        public int PlayerId { get; }
        private readonly object _lock = new object();

        public double UCB1Score(double explorationParameter)
        {
            if (Visits == 0)
                return double.MaxValue;

            double exploitation = TotalScore / Visits;
            double exploration = Math.Sqrt(2 * Math.Log(Parent.Visits) / Visits);
            return exploitation + explorationParameter * exploration;
        }

        public MCTSNode(GameState state, MCTSNode parent = null, GameMove move = null, int playerId = 0)
        {
            State = state;
            Parent = parent;
            Move = move;
            Children = new List<MCTSNode>();
            Visits = 0;
            TotalScore = 0;
            PlayerId = playerId;
        }

        public bool IsFullyExpanded(GameRules rules)
        {
            var validMoves = rules.GetValidMoves(State);
            return Children.Count == validMoves.Count;
        }

        public MCTSNode Expand(GameRules rules)
        {
            var validMoves = rules.GetValidMoves(State);
            var triedMoves = Children.Select(child => child.Move).ToList();
            var untriedMoves = validMoves.Where(move => !triedMoves.Any(m => IsSameMove(m, move))).ToList();

            if (untriedMoves.Count == 0)
                return null;

            var move = untriedMoves[new Random().Next(untriedMoves.Count)];
            var newState = State.Clone();
            rules.ApplyMove(newState, move);

            var childNode = new MCTSNode(newState, this, move, PlayerId);
            Children.Add(childNode);
            return childNode;
        }

        private bool IsSameMove(GameMove move1, GameMove move2)
        {
            if (move1.GetType() != move2.GetType())
                return false;

            if (move1 is PlacementMove pm1 && move2 is PlacementMove pm2)
                return pm1.CharacterId == pm2.CharacterId && pm1.TargetFloor == pm2.TargetFloor;

            if (move1 is AscensionMove am1 && move2 is AscensionMove am2)
                return am1.CharacterId == am2.CharacterId;

            if (move1 is VotingMove vm1 && move2 is VotingMove vm2)
                return vm1.VoteYes == vm2.VoteYes;

            return false;
        }

        public MCTSNode BestChild(double explorationParameter)
        {
            return Children.OrderByDescending(c => c.UCB1Score(explorationParameter)).FirstOrDefault();
        }

        public void Update(double score)
        {
            lock (_lock)
            {
                Visits++;
                TotalScore += score;
            }
        }

        public bool IsTerminal()
        {
            return State.IsGameOver();
        }

        public MCTSNode SelectChild()
        {
            return Children.OrderByDescending(c => c.Visits).FirstOrDefault();
        }
    }
}