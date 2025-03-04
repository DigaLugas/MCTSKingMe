using System;
using System.Collections.Generic;
using System.Linq;

namespace MCTS
{
    public class GameRules
    {
        private readonly Random _random = new Random();

        public List<GameMove> GetValidMoves(GameState state)
        {
            var moves = new List<GameMove>();

            if (state.CurrentPhase == GameState.GamePhase.Placement)
            {
                // Fase de colocação de personagens
                var availableCharacters = state.GetCharactersAvailableForPlacement();
                foreach (var character in availableCharacters)
                {
                    // Pode colocar em qualquer andar de 1 a 4 (Artesãos a Dignitários)
                    for (int floor = 1; floor <= 4; floor++)
                    {
                        if (state.CountCharactersOnFloor((Floor)floor) < 4)
                        {
                            moves.Add(new PlacementMove(character.Id, (Floor)floor));
                        }
                    }
                }
            }
            else if (state.CurrentPhase == GameState.GamePhase.Ascension)
            {
                // Fase de ascensão de personagens
                var availableCharacters = state.GetCharactersAvailableForAscension();
                foreach (var character in availableCharacters)
                {
                    Floor targetFloor = character.CurrentFloor + 1;
                    if (state.CountCharactersOnFloor(targetFloor) < 4 || targetFloor == Floor.Throne)
                    {
                        moves.Add(new AscensionMove(character.Id));
                    }
                }
            }
            else if (state.CurrentPhase == GameState.GamePhase.Voting)
            {
                // Fase de votação
                var player = state.GetCurrentPlayer();

                // Sempre pode votar "Sim"
                moves.Add(new VotingMove(true));

                // Só pode votar "Não" se tiver votos "Não" disponíveis
                if (player.HasNoVotes())
                {
                    moves.Add(new VotingMove(false));
                }
            }

            return moves;
        }

        public void ApplyMove(GameState state, GameMove move)
        {
            switch (move)
            {
                case PlacementMove placementMove:
                    ApplyPlacementMove(state, placementMove);
                    break;
                case AscensionMove ascensionMove:
                    ApplyAscensionMove(state, ascensionMove);
                    break;
                case VotingMove votingMove:
                    ApplyVotingMove(state, votingMove);
                    break;
                default:
                    throw new ArgumentException("Tipo de movimento inválido");
            }
        }

        private void ApplyPlacementMove(GameState state, PlacementMove move)
        {
            var character = state.Characters.FirstOrDefault(c => c.Id == move.CharacterId);
            if (character == null || character.IsEliminated || character.CurrentFloor != Floor.Servants)
                throw new InvalidOperationException("Personagem inválido para colocação");

            character.CurrentFloor = move.TargetFloor;
            state.CharactersPlacedThisRound++;
            state.NextPlayer();

            if (state.IsPlacementPhaseComplete())
            {
                state.TransitionToNextPhase();
            }
        }

        private void ApplyAscensionMove(GameState state, AscensionMove move)
        {
            var character = state.Characters.FirstOrDefault(c => c.Id == move.CharacterId);
            if (character == null || character.IsEliminated)
                throw new InvalidOperationException("Personagem inválido para ascensão");

            Floor targetFloor = character.CurrentFloor + 1;
            if (targetFloor != Floor.Throne && state.CountCharactersOnFloor(targetFloor) >= 4)
                throw new InvalidOperationException("Não há espaço no andar de destino");

            character.CurrentFloor = targetFloor;

            if (targetFloor == Floor.Throne)
            {
                // Se um personagem chega ao trono, vamos para fase de votação
                state.CurrentPhase = GameState.GamePhase.Voting;
            }
            else
            {
                state.NextPlayer();
            }
        }

        private void ApplyVotingMove(GameState state, VotingMove move)
        {
            var player = state.GetCurrentPlayer();

            if (!move.VoteYes && !player.HasNoVotes())
                throw new InvalidOperationException("Jogador não tem votos 'Não' disponíveis");

            if (!move.VoteYes)
            {
                player.UseNoVote();

                // Se algum jogador votar "Não", o personagem é eliminado
                var throneCharacter = state.Characters.FirstOrDefault(c => c.CurrentFloor == Floor.Throne);
                if (throneCharacter != null)
                {
                    throneCharacter.IsEliminated = true;
                    throneCharacter.CurrentFloor = Floor.Servants;
                }

                state.NextPlayer();
                state.CurrentPhase = GameState.GamePhase.Ascension;
            }
            else
            {
                // Se todos votaram "Sim", o rei é coroado e a rodada termina
                bool allVotedYes = true; // Simulação simplificada da votação

                if (allVotedYes)
                {
                    // Calcular pontuação para esta rodada
                    foreach (var p in state.Players)
                    {
                        p.Score += p.CalculateScore(state.Characters);
                    }

                    state.TransitionToNextPhase(); // Vai para o fim da rodada
                }
            }
        }

        public List<int> GenerateRandomFavorites()
        {
            // Gera 6 personagens favoritos aleatórios (índices de 0 a 12)
            var favorites = new List<int>();
            while (favorites.Count < 6)
            {
                int next = _random.Next(13);
                if (!favorites.Contains(next))
                {
                    favorites.Add(next);
                }
            }
            return favorites;
        }

        public void SetupGame(GameState state, int numPlayers)
        {
            // Determina o número de votos "Não" por jogador
            int noVotesPerPlayer;
            switch (numPlayers)
            {
                case 3: noVotesPerPlayer = 4; break;
                case 4: noVotesPerPlayer = 3; break;
                case 5:
                case 6: noVotesPerPlayer = 2; break;
                default: throw new ArgumentException("Número de jogadores inválido");
            }

            // Configura os jogadores
            for (int i = 0; i < numPlayers; i++)
            {
                var favorites = GenerateRandomFavorites();
                state.Players.Add(new Player(i, favorites, noVotesPerPlayer));
            }
        }

        public void SimulateGame(GameState state)
        {
            while (!state.IsGameOver())
            {
                var validMoves = GetValidMoves(state);
                if (validMoves.Count == 0)
                {
                    // Se não houver movimentos válidos, avançamos para a próxima fase
                    state.TransitionToNextPhase();
                    continue;
                }

                // Escolhe um movimento aleatório para simulação
                var move = validMoves[_random.Next(validMoves.Count)];
                ApplyMove(state, move);
            }
        }
    }

    public abstract class GameMove
    {
    }

    public class PlacementMove : GameMove
    {
        public int CharacterId { get; }
        public Floor TargetFloor { get; }

        public PlacementMove(int characterId, Floor targetFloor)
        {
            CharacterId = characterId;
            TargetFloor = targetFloor;
        }
    }

    public class AscensionMove : GameMove
    {
        public int CharacterId { get; }

        public AscensionMove(int characterId)
        {
            CharacterId = characterId;
        }
    }

    public class VotingMove : GameMove
    {
        public bool VoteYes { get; }

        public VotingMove(bool voteYes)
        {
            VoteYes = voteYes;
        }
    }
}