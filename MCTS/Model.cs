using System;
using System.Collections.Generic;
using System.Linq;

namespace MCTS
{
    public enum Floor
    {
        Servants = 0,
        Artisans = 1,
        Merchants = 2,
        Officers = 3,
        Dignitaries = 4,
        Nobles = 5,
        Throne = 6
    }

    public class Character
    {
        public int Id { get; }
        public string Name { get; }
        public Floor CurrentFloor { get; set; }
        public bool IsEliminated { get; set; }

        public Character(int id, string name)
        {
            Id = id;
            Name = name;
            CurrentFloor = Floor.Servants;
            IsEliminated = false;
        }

        public Character Clone()
        {
            return new Character(Id, Name)
            {
                CurrentFloor = CurrentFloor,
                IsEliminated = IsEliminated
            };
        }
    }

    public class Player
    {
        public int Id { get; }
        public List<int> FavoriteCharacters { get; }
        public int YesVotes { get; set; }
        public int NoVotes { get; set; }
        public int Score { get; set; }

        public Player(int id, List<int> favoriteCharacters, int noVotes)
        {
            Id = id;
            FavoriteCharacters = favoriteCharacters;
            YesVotes = 1; // Sempre tem 1 voto "Sim"
            NoVotes = noVotes;
            Score = 0;
        }

        public Player Clone()
        {
            var player = new Player(Id, new List<int>(FavoriteCharacters), NoVotes)
            {
                YesVotes = YesVotes,
                Score = Score
            };
            return player;
        }

        public bool HasNoVotes()
        {
            return NoVotes > 0;
        }

        public void UseNoVote()
        {
            if (NoVotes <= 0)
                throw new InvalidOperationException("Não há votos 'Não' disponíveis");
            NoVotes--;
        }

        public int CalculateScore(List<Character> characters)
        {
            int roundScore = 0;
            foreach (var charId in FavoriteCharacters)
            {
                var character = characters.FirstOrDefault(c => c.Id == charId);
                if (character != null && !character.IsEliminated)
                {
                    roundScore += GetScoreForFloor(character.CurrentFloor);
                }
            }
            return roundScore;
        }

        private int GetScoreForFloor(Floor floor)
        {
            switch (floor)
            {
                case Floor.Throne: return 10;
                case Floor.Nobles: return 5;
                case Floor.Dignitaries: return 4;
                case Floor.Officers: return 3;
                case Floor.Merchants: return 2;
                case Floor.Artisans: return 1;
                case Floor.Servants:
                default: return 0;
            }
        }
    }

    public class GameState
    {
        public List<Character> Characters { get; }
        public List<Player> Players { get; }
        public int CurrentRound { get; set; }
        public int CurrentPlayerIndex { get; set; }
        public GamePhase CurrentPhase { get; set; }
        public int CharactersPlacedThisRound { get; set; }
        public int CharactersToPlacePerPlayer { get; }

        public enum GamePhase
        {
            Placement,
            Ascension,
            Voting,
            EndRound
        }

        public GameState(int numPlayers)
        {
            Characters = InitializeCharacters();
            Players = new List<Player>();
            CurrentRound = 1;
            CurrentPlayerIndex = 0;
            CurrentPhase = GamePhase.Placement;
            CharactersPlacedThisRound = 0;


            switch (numPlayers)
            {
                case 3: CharactersToPlacePerPlayer = 4; break;
                case 4: CharactersToPlacePerPlayer = 3; break;
                case 5:
                case 6: CharactersToPlacePerPlayer = 2; break;
                default: throw new ArgumentException("Número de jogadores inválido");
            }
        }

        private List<Character> InitializeCharacters()
        {
            return new List<Character>
            {
                new Character(0, "Alighiero o Escudeiro"),
                new Character(1, "Beatrice a Encantadora"),
                new Character(2, "Clemente o Sargento"),
                new Character(3, "Dario o Antiquário"),
                new Character(4, "Ernesto o Duque"),
                new Character(5, "Fiorello o Pintor"),
                new Character(6, "Gavino o Paladino"),
                new Character(7, "Irina a Fazendeira"),
                new Character(8, "Leonardo o Mensageiro"),
                new Character(9, "Merlino o Vidente"),
                new Character(10, "Natale o Guardião"),
                new Character(11, "Odessa a Condessa"),
                new Character(12, "Piero o Cozinheiro")
            };
        }

        public bool IsGameOver()
        {
            return CurrentRound > 3;
        }

        public GameState Clone()
        {
            var clonedState = new GameState(Players.Count)
            {
                CurrentRound = CurrentRound,
                CurrentPlayerIndex = CurrentPlayerIndex,
                CurrentPhase = CurrentPhase,
                CharactersPlacedThisRound = CharactersPlacedThisRound
            };

            clonedState.Characters.Clear();
            foreach (var character in Characters)
            {
                clonedState.Characters.Add(character.Clone());
            }

            clonedState.Players.Clear();
            foreach (var player in Players)
            {
                clonedState.Players.Add(player.Clone());
            }

            return clonedState;
        }

        public bool IsPlacementPhaseComplete()
        {
            return CharactersPlacedThisRound >= Players.Count * CharactersToPlacePerPlayer;
        }

        public int CountCharactersOnFloor(Floor floor)
        {
            return Characters.Count(c => c.CurrentFloor == floor && !c.IsEliminated);
        }

        public List<Character> GetCharactersAvailableForPlacement()
        {
            return Characters.Where(c => c.CurrentFloor == Floor.Servants && !c.IsEliminated).ToList();
        }

        public List<Character> GetCharactersAvailableForAscension()
        {
            return Characters.Where(c => !c.IsEliminated && c.CurrentFloor < Floor.Throne &&
                                          CountCharactersOnFloor((Floor)((int)c.CurrentFloor + 1)) < 4).ToList();
        }

        public Player GetCurrentPlayer()
        {
            return Players[CurrentPlayerIndex];
        }

        public void NextPlayer()
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
        }

        public void TransitionToNextPhase()
        {
            switch (CurrentPhase)
            {
                case GamePhase.Placement:
                    CurrentPhase = GamePhase.Ascension;
                    break;
                case GamePhase.Ascension:

                    CurrentPhase = GamePhase.EndRound;
                    break;
                case GamePhase.Voting:

                    CurrentPhase = GamePhase.Ascension;
                    break;
                case GamePhase.EndRound:
                    CurrentRound++;
                    if (CurrentRound <= 3)
                    {
                        ResetForNewRound();
                        CurrentPhase = GamePhase.Placement;
                    }
                    break;
            }
        }

        private void ResetForNewRound()
        {
            foreach (var character in Characters)
            {
                character.CurrentFloor = Floor.Servants;
                character.IsEliminated = false;
            }
            CharactersPlacedThisRound = 0;
            CurrentPlayerIndex = 0;
        }
    }
}