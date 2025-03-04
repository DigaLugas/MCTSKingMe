using MCTS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MCTS
{
    public class MCTSParallel
    {
        private readonly GameRules _rules;
        private readonly int _maxIterations;
        private readonly double _explorationParameter;
        private readonly int _numThreads;
        private readonly Random _random = new Random();
        private readonly object _rootLock = new object();

        public MCTSParallel(GameRules rules, int maxIterations = 10000, double explorationParameter = 1.414, int numThreads = 4)
        {
            _rules = rules;
            _maxIterations = maxIterations;
            _explorationParameter = explorationParameter;
            _numThreads = numThreads;
        }

        public GameMove FindBestMove(GameState state, int playerId)
        {
            var root = new MCTSNode(state.Clone(), null, null, playerId);
            var iterationsPerThread = _maxIterations / _numThreads;
            var tasks = new Task[_numThreads];

            // Criando as tarefas paralelas
            for (int i = 0; i < _numThreads; i++)
            {
                int threadId = i;
                tasks[i] = Task.Run(() => ParallelSearch(root, iterationsPerThread, threadId));
            }

            // Aguardando todas as tarefas concluírem
            Task.WaitAll(tasks);

            // Selecionando o melhor filho
            var bestChild = root.SelectChild();
            return bestChild?.Move;
        }

        private void ParallelSearch(MCTSNode root, int iterations, int threadId)
        {
            for (int i = 0; i < iterations; i++)
            {
                // Fase de seleção
                var selectedNode = Selection(root);

                // Fase de expansão e simulação
                if (!selectedNode.IsTerminal())
                {
                    // Se o nó não estiver totalmente expandido, expanda-o
                    MCTSNode nodeToExplore;

                    lock (_rootLock)
                    {
                        if (!selectedNode.IsFullyExpanded(_rules))
                        {
                            nodeToExplore = selectedNode.Expand(_rules);
                        }
                        else
                        {
                            nodeToExplore = selectedNode.BestChild(_explorationParameter);
                        }
                    }

                    if (nodeToExplore != null)
                    {
                        // Fase de simulação
                        var simulationResult = Simulation(nodeToExplore);

                        // Fase de backpropagation
                        Backpropagation(nodeToExplore, simulationResult);
                    }
                }
            }
        }

        private MCTSNode Selection(MCTSNode node)
        {
            while (!node.IsTerminal() && node.IsFullyExpanded(_rules))
            {
                var bestChild = node.BestChild(_explorationParameter);
                if (bestChild == null)
                    break;

                node = bestChild;
            }
            return node;
        }

        private double Simulation(MCTSNode node)
        {
            // Clone o estado para não afetar a árvore original
            var simulationState = node.State.Clone();
            int playerId = node.PlayerId;

            // Simula o jogo até o final
            while (!simulationState.IsGameOver())
            {
                var validMoves = _rules.GetValidMoves(simulationState);
                if (validMoves.Count == 0)
                {
                    simulationState.TransitionToNextPhase();
                    continue;
                }

                // Escolhe um movimento aleatório para simulação
                var randomMove = validMoves[_random.Next(validMoves.Count)];
                _rules.ApplyMove(simulationState, randomMove);
            }

            // Avalia o estado final para o jogador atual
            return EvaluateState(simulationState, playerId);
        }

        private void Backpropagation(MCTSNode node, double score)
        {
            while (node != null)
            {
                node.Update(score);
                node = node.Parent;
            }
        }

        private double EvaluateState(GameState state, int playerId)
        {
            if (playerId < 0 || playerId >= state.Players.Count)
                return 0;

            // Obtém a pontuação do jogador atual
            var player = state.Players[playerId];

            // Normaliza a pontuação para algo entre 0 e 1
            // O máximo teórico de pontos em uma rodada é 33
            return player.Score / 99.0; // 33 pontos * 3 rodadas = 99 pontos máximos
        }
    }
}