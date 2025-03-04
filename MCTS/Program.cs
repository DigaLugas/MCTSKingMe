using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using MCTS;

namespace KingMe
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("King Me! - Implementação MCTS Paralelizada");
            Console.WriteLine("==========================================");

            int numPlayers = 4; // 3 a 6 jogadores
            int numIterationsMCTS = 5000;
            int numThreads = Environment.ProcessorCount; // Usa todos os núcleos disponíveis

            Console.WriteLine($"Número de jogadores: {numPlayers}");
            Console.WriteLine($"Iterações MCTS: {numIterationsMCTS}");
            Console.WriteLine($"Threads utilizadas: {numThreads}");
            Console.WriteLine();

            // Cria o estado inicial do jogo
            var gameState = new GameState(numPlayers);
            var rules = new GameRules();
            rules.SetupGame(gameState, numPlayers);

            // Cria um agente MCTS para cada jogador
            var agents = new List<MCTSAgent>();
            for (int i = 0; i < numPlayers; i++)
            {
                agents.Add(new MCTSAgent(i, numIterationsMCTS, numThreads));
            }

            // Executa o jogo
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (!gameState.IsGameOver())
            {
                Console.WriteLine($"Rodada {gameState.CurrentRound}, Fase: {gameState.CurrentPhase}");

                // Turno do jogador atual
                int currentPlayerIndex = gameState.CurrentPlayerIndex;
                var currentPlayer = gameState.Players[currentPlayerIndex];
                var agent = agents[currentPlayerIndex];

                Console.WriteLine($"Jogador {currentPlayerIndex + 1} está pensando...");

                var validMoves = agent.GetValidMoves(gameState);
                if (validMoves.Count == 0)
                {
                    Console.WriteLine("Sem movimentos válidos disponíveis. Avançando para a próxima fase.");
                    gameState.TransitionToNextPhase();
                    continue;
                }

                var move = agent.MakeMove(gameState);

                if (move == null)
                {
                    Console.WriteLine("Agente não conseguiu determinar um movimento. Avançando para a próxima fase.");
                    gameState.TransitionToNextPhase();
                    continue;
                }

                // Exibe o movimento escolhido
                DisplayMove(gameState, move);

                // Aplica o movimento
                agent.ApplyMove(gameState, move);

                // Exibe o estado atual do castelo
                DisplayCastle(gameState);

                Console.WriteLine();
            }

            stopwatch.Stop();

            // Exibe o resultado final
            DisplayFinalResults(gameState);

            Console.WriteLine($"Tempo total de execução: {stopwatch.ElapsedMilliseconds / 1000.0:F2} segundos");
            Console.WriteLine("Jogo concluído.");
        }

        static void DisplayMove(GameState state, GameMove move)
        {
            var player = state.GetCurrentPlayer();

            if (move is PlacementMove placementMove)
            {
                var character = state.Characters.FirstOrDefault(c => c.Id == placementMove.CharacterId);
                Console.WriteLine($"Jogador {player.Id + 1} colocou {character.Name} no andar {placementMove.TargetFloor}");
            }
            else if (move is AscensionMove ascensionMove)
            {
                var character = state.Characters.FirstOrDefault(c => c.Id == ascensionMove.CharacterId);
                Console.WriteLine($"Jogador {player.Id + 1} moveu {character.Name} do andar {character.CurrentFloor} para o andar {character.CurrentFloor + 1}");

                if (character.CurrentFloor + 1 == Floor.Throne)
                {
                    Console.WriteLine($"{character.Name} alcançou o trono! Votação será realizada.");
                }
            }
            else if (move is VotingMove votingMove)
            {
                var vote = votingMove.VoteYes ? "SIM" : "NÃO";
                Console.WriteLine($"Jogador {player.Id + 1} votou {vote}");

                if (!votingMove.VoteYes)
                {
                    var throneCharacter = state.Characters.FirstOrDefault(c => c.CurrentFloor == Floor.Throne);
                    if (throneCharacter != null)
                    {
                        Console.WriteLine($"{throneCharacter.Name} foi rejeitado e eliminado desta rodada!");
                    }
                }
                else
                {
                    Console.WriteLine("Rei coroado! Rodada finalizada.");
                }
            }
        }

        static void DisplayCastle(GameState state)
        {
            Console.WriteLine("\nEstado atual do castelo:");
            Console.WriteLine("------------------------");

            for (int floor = 6; floor >= 0; floor--)
            {
                string floorName = ((Floor)floor).ToString();
                Console.Write($"{floorName} ({floor}): ");

                var charactersOnFloor = state.Characters
                    .Where(c => !c.IsEliminated && c.CurrentFloor == (Floor)floor)
                    .OrderBy(c => c.Id)
                    .ToList();

                if (charactersOnFloor.Count == 0)
                {
                    Console.WriteLine("(vazio)");
                }
                else
                {
                    Console.WriteLine(string.Join(", ", charactersOnFloor.Select(c => c.Name)));
                }
            }

            // Exibe personagens eliminados
            var eliminatedCharacters = state.Characters.Where(c => c.IsEliminated).ToList();
            if (eliminatedCharacters.Count > 0)
            {
                Console.WriteLine("\nPersonagens eliminados:");
                Console.WriteLine(string.Join(", ", eliminatedCharacters.Select(c => c.Name)));
            }

            Console.WriteLine();
        }

        static void DisplayFinalResults(GameState state)
        {
            Console.WriteLine("\nResultado Final");
            Console.WriteLine("===============");

            var sortedPlayers = state.Players.OrderByDescending(p => p.Score).ToList();

            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                var player = sortedPlayers[i];
                Console.WriteLine($"Posição {i + 1}: Jogador {player.Id + 1} - {player.Score} pontos");

                Console.WriteLine("Personagens favoritos:");
                foreach (var charId in player.FavoriteCharacters)
                {
                    var character = state.Characters.FirstOrDefault(c => c.Id == charId);
                    Console.WriteLine($"- {character.Name}");
                }
                Console.WriteLine();
            }
        }
    }
}