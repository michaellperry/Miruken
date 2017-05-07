namespace Miruken.Mediator
{
    using Callback;
    using Callback.Policy;

    public class MediatesAttribute : DefinitionAttribute
    {
        public MediatesAttribute()
        {          
        }

        public MediatesAttribute(object key)
        {
            Key = key;
        }

        public override CallbackPolicy CallbackPolicy => Policy;

        public static readonly ContravariantPolicy Policy =
            ContravariantPolicy.Create(
                x => x.MatchMethod(x.Callback.OfType<IRequest>())
                      .MatchMethod(Return.Is("resp"), x.Callback.OfType(typeof(IRequest<>), "resp"))
            );
    }
}
