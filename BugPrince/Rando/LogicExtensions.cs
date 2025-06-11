using BugPrince.Data;
using BugPrince.IC.Items;
using BugPrince.Util;
using RandomizerCore.Logic;

namespace BugPrince.Rando;

internal static class LogicExtensions
{
    private static string GetTermString(this CostType self) => self switch { CostType.Coins => CoinItem.TERM_NAME, CostType.Gems => GemItem.TERM_NAME, _ => throw self.InvalidEnum() };

    internal static void AddCostTypeTerms(this LogicManagerBuilder self)
    {
        self.GetOrAddTerm(CoinItem.TERM_NAME);
        self.GetOrAddTerm(GemItem.TERM_NAME);
    }

    internal static Term GetTerm(this CostType self, LogicManager lm) => lm.GetTermStrict(self.GetTermString());

    internal static bool TryGetInner<T>(this VariableResolver self, out T inner)
    {
        VariableResolver? v = self;
        while (self != null)
        {
            if (v is T t)
            {
                inner = t;
                return true;
            }

            v = self.Inner;
        }

        inner = default;
        return false;
    }
}
