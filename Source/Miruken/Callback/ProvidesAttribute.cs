﻿namespace Miruken.Callback
{
    using Policy;

    public class ProvidesAttribute : DefinitionAttribute
    {
        public ProvidesAttribute()
        {
        }

        public ProvidesAttribute(object key)
        {
            Key = key;
        }

        public override CallbackPolicy CallbackPolicy => Policy;

        public static readonly CallbackPolicy Policy =
             CovariantPolicy.Create<Inquiry>(r => r.Key,
                x => x.MatchMethod(x.ReturnKey.OrVoid,  x.Callback,
                                   x.Composer.Optional, x.Binding.Optional)
                      .MatchMethod(x.ReturnKey, x.Composer.Optional, x.Binding.Optional)
                );
    }
}
