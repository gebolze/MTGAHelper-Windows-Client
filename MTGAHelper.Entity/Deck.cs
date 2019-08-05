﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace MTGAHelper.Entity
{
    public interface IDeck
    {
        string Name { get; set; }
        ScraperType ScraperType { get; }
        string DisplayMember { get; }
        DeckCards Cards { get; }

        string Id { get; }

        string GetColor();

        bool FilterName(string filter);
        bool FilterColor(string filter);
        bool FilterSource(string source, string filter);
        bool FilterCard(string filter);
    }

    public abstract class DeckBase : IDeck
    {
        Util util = new Util();

        protected DeckBase(string name, ScraperType scraperType)
        {
            Name = name;
            ScraperType = scraperType;
        }

        public string Name { get; set; }
        public ScraperType ScraperType { get; protected set; }

        public DeckCards Cards { get; protected set; }

        public string DisplayMember { get { return $"{Name}"; } }

        public string Id { get; protected set; }

        protected string GetId()
        {
            var cardsMain = Cards.QuickCardsMain.Values.GroupBy(i => i.Card.name).Select(i => $"{i.Sum(x => x.Amount)} {i.Key}");
            var cardsSideboard = Cards.QuickCardsSideboard.Values.GroupBy(i => i.Card.name).Select(i => $"{i.Sum(x => x.Amount)} {i.Key}");

            var m = string.Join("_", cardsMain);
            var s = string.Join("_", cardsSideboard);
            var id = util.GetHash($"{m}|{s}");

            return ScraperType + id;
        }

        public string GetColor()
        {
            var order = new Dictionary<string, int> {
                { "W", 1 },
                { "U", 2 },
                { "B", 3 },
                { "R", 4 },
                { "G", 5 },
            };
            var colors = Cards.All
                .Where(i => i.Card.colors != null)
                .SelectMany(i => i.Card.colors)
                .Distinct()
                .OrderBy(i => order[i]);

            return string.Join("", colors);
        }

        public bool FilterSource(string scraperTypeId, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            return
                scraperTypeId.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                //|| filter == "(All)"
                //|| (filter == "(Unknown)" && string.IsNullOrWhiteSpace(source))
                ;
        }

        public bool FilterColor(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            var filterUpperCase = filter.ToUpper();

            var colors = new List<string>();

            if (filterUpperCase.Contains("W"))
                colors.Add("W");
            if (filterUpperCase.Contains("U"))
                colors.Add("U");
            if (filterUpperCase.Contains("B"))
                colors.Add("B");
            if (filterUpperCase.Contains("R"))
                colors.Add("R");
            if (filterUpperCase.Contains("G"))
                colors.Add("G");

            if (colors.Count == 0)
            {
                // No filter
                return true;
            }
            else if (colors.Count == 1)
            {
                // Mono...we check for at least 80% cards of that single color
                var minNbCards = (int)(this.Cards.All.Count * 0.8f);
                var color = colors.Single();
                var nbCards = this.Cards.All
                    .Count(i => i.Card.colors == null || i.Card.colors.Count == 1 && i.Card.colors.Single() == color);
                return nbCards >= minNbCards;
            }
            else
            {
                return this.Cards.All
                    .Where(i => i.Card.colors != null)
                    .Where(i => i.IsSideboard == false)
                    .All(i => i.Card.colors.All(c => colors.Contains(c)));
            }
        }

        public bool FilterName(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            return this.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public bool FilterCard(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            return this.Cards.All.Any(i => i.Card.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }

    public class Deck : DeckBase
    {
        public Deck(string name, ScraperType scraperTypeId, ICollection<DeckCard> cards)
            : base(name, scraperTypeId)
        {
            Cards = new DeckCards(cards);
            Id = GetId();
        }

        //public override float CalcMissingWeight() { return Cards.Sum(i => i.MissingWeight); }

        //public override void ApplyCompareResult(Card card, bool isSideboard, int nbMissing, float missingWeight, int nbOwned)
        //{
        //    var c = Cards.Single(i => i.Card == card && i.IsSideboard == isSideboard);
        //    c.ApplyCompareResult(nbMissing, missingWeight);
        //}
    }
}