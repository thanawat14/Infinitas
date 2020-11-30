using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Infinitas
{
    [Event("Transfer")]
    public class TransferEvent : IEventDTO
    {
        [Parameter("address", "from", 1, true)]
        public string From { get; set; }
        [Parameter("address", "to", 2, true)]
        public string To { get; set; }
        [Parameter("uint256", "ref", 3, true)]
        public BigInteger Ref { get; set; }
        [Parameter("uint256", "value", 4)]
        public ulong Value { get; set; }
    }
}