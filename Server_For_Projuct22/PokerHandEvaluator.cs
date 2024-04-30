using Server_for_projuct2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Server_For_Projuct22
{
    /// <summary>
    /// Represents a playing card.
    /// </summary>
    public class Card
    {
        /// <summary>
        /// The suit of the card (e.g., "Spade", "Heart", "Diamond", "Club").
        /// </summary>
        public string Suit { get; }
        /// <summary>
        /// The numerical value of the card.
        /// </summary>
        public int Value { get; }
        /// <summary>
        /// Initializes a new instance of the Card class.
        /// </summary>
        /// <param name="suit">The suit of the card.</param>
        /// <param name="value">The value of the card.</param>
        public Card(string suit, int value)
        {
            Suit = suit;
            Value = value;
        }
    }
    /// <summary>
    /// Represents the strength of a poker hand.
    /// </summary>
    public class HandStrength
    {
        /// <summary>
        /// The Main strength of the hand.
        /// </summary>
        public int Strength { get; set; }
        /// <summary>
        /// The sub-strength of the hand (if needed for specific hand comparisons).
        /// </summary>
        public int SubStrength { get; set; }
        /// <summary>
        /// Initializes a new instance of the HandStrength class.
        /// </summary>
        /// <param name="strength">The overall strength of the hand.</param>
        /// <param name="subStrength">The sub-strength of the hand.</param>
        public HandStrength(int strength, int subStrength)
        {
            Strength = strength;
            SubStrength = subStrength;
        }
    }
    /// <summary>
    /// Provides methods to parse cards and evaluate poker hands.
    /// </summary>
    public class PokerHandEvaluator
    {
        /// <summary>
        /// Parses a card string into a Card object.
        /// </summary>
        /// <param name="cardStr">The string representation of the card (e.g., "Spade:10").</param>
        /// <returns>The parsed Card object.</returns>
        public static Card ParseCard(string cardStr)
        {
            string[] parts = cardStr.Split(':');
            string suit = parts[0];
            int value = int.Parse(parts[1]);
            return new Card(suit, value);
        }
        /// <summary>
        /// Finds the index(es) of the strongest hand(s) in an array of HandStrength objects.
        /// </summary>
        /// <param name="HSArray">The array of HandStrength objects representing different hands.</param>
        /// <returns>An array of integers representing the index(es) of the strongest hand(s).</returns>
        public static int[] FindStrongestHand(HandStrength[] HSArray)
        {
            List<int> winningIndex = new List<int>();
            winningIndex.Add(0); // Start with the assumption that the first hand is the strongest
            for (int i = 1; i < HSArray.Length; i++)
            {
                if (HSArray[0] == null)
                {
                    if (HSArray[i] != null)
                    {
                        winningIndex.Clear();
                        winningIndex.Add(i);
                    }
                }
                else if(HSArray[i] != null)
                {
                    int compareResult = CompareHands(HSArray[i], HSArray[winningIndex[0]]);
                    if (compareResult > 0)
                    {
                        winningIndex.Clear(); // Clear previous index
                        winningIndex.Add(i); // Add current index as the strongest
                    }
                    else if (compareResult == 0)
                    {
                        winningIndex.Add(i); // Add current index in case of a tie
                    }
                }
            }
            return winningIndex.ToArray();
        }
        /// <summary>
        /// Compares two HandStrength objects to determine which hand is stronger.
        /// </summary>
        /// <param name="hand1">The first HandStrength object to compare.</param>
        /// <param name="hand2">The second HandStrength object to compare.</param>
        /// <returns>
        /// 1 if hand1 is stronger, -1 if hand2 is stronger, and 0 if the hands are tied.
        /// </returns>
        private static int CompareHands(HandStrength hand1, HandStrength hand2)
        {
            if (hand1.Strength > hand2.Strength ||
                (hand1.Strength == hand2.Strength && hand1.SubStrength > hand2.SubStrength))
            {
                return 1; // hand1 is stronger
            }
            else if (hand1.Strength < hand2.Strength ||
                     (hand1.Strength == hand2.Strength && hand1.SubStrength < hand2.SubStrength))
            {
                return -1; // hand2 is stronger
            }
            else
            {
                return 0; // hands are tied
            }
        }
        /// <summary>
        /// Evaluates the overall strength of a poker hand consisting of player's hand and table cards.
        /// </summary>
        /// <param name="playerHand">Array of strings representing the player's hand cards.</param>
        /// <param name="tableCards">Array of strings representing the table cards.</param>
        /// <returns>
        /// A <see cref="HandStrength"/> object indicating the type and strength of the evaluated hand.
        /// </returns>
        public static HandStrength EvaluateHand(string[] playerHand, string[] tableCards)
        {
            List<Card> cards = new List<Card>(); // all the cards
            List<Card> Playercards = new List<Card>(); // the player's cards
            foreach (string cardStr in playerHand)
            {
                Playercards.Add(ParseCard(cardStr));
            }
            foreach (string cardStr in playerHand.Concat(tableCards))
            {
                cards.Add(ParseCard(cardStr));
            }
            if (IsFlush(cards) > 0 && IsStraight(cards) > 0) // is a royal flush.
                return new HandStrength(9, IsStraight(cards));
            if (IsFlush(cards) > 0) // is a flush.
                return new HandStrength(6, IsFlush(cards));
            if(IsStraight(cards) > 0) // is a straight.
                return new HandStrength(5, IsStraight(cards));
            return EvaluateHandType(cards, Playercards); // all other hands.
        }
        /// <summary>
        /// Checks if the given list of cards contains a flush (five or more cards of the same suit).
        /// </summary>
        /// <param name="cards">The list of cards to evaluate.</param>
        /// <returns>
        /// The value of the highest card in the flush if a flush is found, or 0 if no flush is present.
        /// </returns>
        public static int IsFlush(List<Card> cards)
        {
            // Count the number of cards for each suit
            var suitCounts = new Dictionary<string, int>();
            foreach (var card in cards)
            {
                if (!suitCounts.ContainsKey(card.Suit))
                    suitCounts[card.Suit] = 1;
                else
                    suitCounts[card.Suit]++;
            }
            // Check if any suit has 5 or more cards, indicating a flush
            foreach (var suitCount in suitCounts)
            {
                if (suitCount.Value >= 5)
                    // Get the highest card of the flush suit
                    return cards.Where(c => c.Suit == suitCount.Key).Select(c => c.Value).Max();
            }
            return 0; // No flush
        }
        /// <summary>
        /// Checks if the given list of cards contains a straight (five consecutive cards in value order).
        /// </summary>
        /// <param name="cards">The list of cards to evaluate.</param>
        /// <returns>
        /// The value of the highest card in the straight if a straight is found, or 0 if no straight is present.
        /// </returns>
        public static int IsStraight(List<Card> cards)
        {
            // Sort the cards by their values in ascending order
            cards.Sort((a, b) => a.Value.CompareTo(b.Value));
            // Check for Ace-low straight (Ace can be both high and low)
            if (cards[0].Value == 2 && cards[cards.Count - 1].Value == 14)
            {
                // Check if the remaining cards form a straight (excluding Ace)
                for (int i = 1; i < cards.Count - 1; i++)
                {
                    if (cards[i].Value != cards[i - 1].Value + 1)
                        return 0; // Not a straight
                }
                return 5; // Ace-low straight, return the highest card (Five)
            }
            // Check if the cards form a straight (excluding Ace)
            for (int i = 1; i < cards.Count; i++)
            {
                if (cards[i].Value != cards[i - 1].Value + 1)
                    return 0; // Not a straight
            }
            return cards[cards.Count - 1].Value; // Regular straight, return the highest card
        }
        /// <summary>
        /// Evaluates the type and strength of a poker hand based on the provided cards.
        /// </summary>
        /// <param name="cards">List of cards to evaluate.</param>
        /// <param name="PlayerCards">List of the player's cards for high card tie-breaking purposes.</param>
        /// <returns>
        /// A <see cref="HandStrength"/> object indicating the type and strength of the evaluated hand.
        /// </returns>
        public static HandStrength EvaluateHandType(List<Card> cards, List<Card> PlayerCards)
        {
            var valueCounts = cards.GroupBy(c => c.Value) // Counts how much cards there are for each value
                                   .ToDictionary(g => g.Key, g => g.Count());
            bool hasThreeOfAKind = false;
            bool hasPair = false;
            int highestCardValue = 0;
            foreach (var pair in valueCounts)
            {
                if (pair.Value == 4)
                    return new HandStrength(8, pair.Key); // Four of a kind
                if (pair.Value == 3)
                {
                        hasThreeOfAKind = true;
                        highestCardValue = pair.Key; // Store the value of the three of a kind
                }
                else if (pair.Value == 2)
                {
                    if (hasPair)
                    {
                        highestCardValue = Math.Max(highestCardValue, pair.Key); // Update highest card value for two pairs
                        return new HandStrength(3, highestCardValue); // Two pair
                    }
                    else
                    {
                        hasPair = true;
                        highestCardValue = Math.Max(highestCardValue, pair.Key); // Update highest card value for pair
                    }
                }
            }
            if (hasThreeOfAKind&&hasPair)
            {
                return new HandStrength(7, highestCardValue); // Full house
            }
            else if (hasThreeOfAKind)
            {
                return new HandStrength(4, highestCardValue); // Three of a kind
            }
            else if (hasPair)
            {
                return new HandStrength(2, highestCardValue); // Pair
            }
            else
            {
                return new HandStrength(1, PlayerCards.Max(card => card.Value));// High card (default)
            }
        }
    }
}
