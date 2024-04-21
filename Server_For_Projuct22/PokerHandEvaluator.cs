using Server_for_projuct2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_For_Projuct22
{
    public class Card
    {
        public string Suit { get; }
        public int Value { get; }
        public Card(string suit, int value)
        {
            Suit = suit;
            Value = value;
        }
    }
    public class HandStrength 
    { 
        public int Strength { get; set; }
        public int SubStrength { get; set; }
        public HandStrength(int strength,int subStrength) 
        { 
            Strength = strength;
            SubStrength = subStrength;
        }
    }
    public class PokerHandEvaluator
    {
        public static Card ParseCard(string cardStr)
        {
            string[] parts = cardStr.Split(':');
            string suit = parts[0];
            int value = int.Parse(parts[1]);
            return new Card(suit, value);
        }
        public static int FindStrongestHand(HandStrength[] HSArray)
        {
            int Index = 0;
            for(int i=1;i< HSArray.Length;i++)
            {
                if (HSArray[0] == null)
                {
                    if (HSArray[i] != null)
                        Index = i;
                }
                else if (HSArray[i] != null && (HSArray[i].Strength > HSArray[Index].Strength ||
                    (HSArray[i].Strength == HSArray[Index].Strength && HSArray[i].SubStrength > HSArray[Index].SubStrength)))
                    Index = i;
            }
            return Index;
        }
        public static HandStrength EvaluateHand(string[] playerHand, string[] tableCards)
        {
            List<Card> cards = new List<Card>();
            foreach (string cardStr in playerHand.Concat(tableCards))
            {
                cards.Add(ParseCard(cardStr));
            }
            if(IsFlush(cards) > 0 && IsStraight(cards) > 0)
                return new HandStrength(9, IsStraight(cards));
            if (IsFlush(cards) > 0)
                return new HandStrength(6, IsFlush(cards));
            if(IsStraight(cards) > 0)
                return new HandStrength(5, IsStraight(cards));
            return new HandStrength(int.Parse(EvaluateHandType(cards).Split(':')[0]), int.Parse(EvaluateHandType(cards).Split(':')[1]));
        }
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
        public static string EvaluateHandType(List<Card> cards)
        {
            var valueCounts = cards.GroupBy(c => c.Value) // Counts how much cards there are for each value
                                   .ToDictionary(g => g.Key, g => g.Count());
            bool hasThreeOfAKind = false;
            bool hasPair = false;
            int highestCardValue = 0;
            int highestCardValueNoPair = 0;
            foreach (var pair in valueCounts)
            {
                if (pair.Value == 4)
                    return "8:"+pair.Key; // Four of a kind
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
                        return "3:" + highestCardValue; // Two pair
                    }
                    else
                    {
                        hasPair = true;
                        highestCardValue = Math.Max(highestCardValue, pair.Key); // Update highest card value for pair
                    }
                }
                else
                {
                    highestCardValueNoPair = Math.Max(highestCardValue, pair.Key); // Update highest card value for other cards
                }
            }
            if (hasThreeOfAKind&&hasPair)
            {
                return "7:" + highestCardValue; // Full house
            }
            else if (hasThreeOfAKind)
            {
                return "4:" + highestCardValue; // Three of a kind
            }
            else if (hasPair)
            {
                return "2:" + highestCardValue; // Pair
            }
            else
            {
                return "1:" + highestCardValueNoPair; // High card (default)
            }
        }
    }
}
