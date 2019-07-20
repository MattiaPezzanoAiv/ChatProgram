using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatService.Packets;
using NUnit.Framework;

namespace PacketTests
{
    [TestFixture]
    public class PacketTests
    {
        [Test]
        public void PacketCast()
        {
            ProtocolObject.Join join = new ProtocolObject.Join()
            {
                name = "FooBar"
            };
            Protocol p = join.Proto;

            Assert.That(p, Is.EqualTo(Protocol.JOIN));
            ProtocolObject.Quit quit = new ProtocolObject.Quit()
            {
                name = "FooBar"
            };
            Protocol p2 = quit.Proto;

            Assert.That(p2, Is.EqualTo(Protocol.QUIT));

            ProtocolObject.AskAllConnected ask = new ProtocolObject.AskAllConnected()
            {
            };
            Protocol p3 = ask.Proto;

            Assert.That((int)p3, Is.EqualTo((int)Protocol.ASK_ALL_CONNECTED));

            ProtocolObject.BaseProtocolObject baseObj = new ProtocolObject.Join()
            {
                name = "FooBar"
            };
            Protocol pBase = baseObj.Proto;
            Assert.That(pBase, Is.EqualTo(Protocol.JOIN));
        }

        [Test]
        public void PacketCreationAndParsing()
        {
            ProtocolObject.Join join = new ProtocolObject.Join()
            {
                name = "FooBar"
            };
            var buffer = PacketUtilities.Build(join);   //should automatically create all

            Assert.That(buffer, Is.Not.Null);
            Assert.That(buffer.Length, Is.GreaterThan(0));

            var parsedPacket = PacketUtilities.Read(buffer);

            Assert.That(parsedPacket, Is.Not.Null);
            Assert.That(parsedPacket.command, Is.EqualTo((int)Protocol.JOIN));
            Assert.That(parsedPacket.json, Is.Not.Null);
            Assert.That(string.IsNullOrEmpty(parsedPacket.json), Is.False);

            var reparsedJoin = PacketUtilities.GetProtocolObject<ProtocolObject.Join>(parsedPacket);

            Assert.That(reparsedJoin, Is.Not.Null);
            Assert.That(reparsedJoin.name, Is.EqualTo("FooBar"));
        }

        [Test]
        public void PacketCreationAndParsingEmpty()
        {
            ProtocolObject.AskAllConnected ask = new ProtocolObject.AskAllConnected();

            var buffer = PacketUtilities.Build(ask);   //should automatically create all

            Assert.That(buffer, Is.Not.Null);
            Assert.That(buffer.Length, Is.GreaterThan(0));

            var parsedPacket = PacketUtilities.Read(buffer);

            Assert.That(parsedPacket, Is.Not.Null);
            Assert.That(parsedPacket.command, Is.EqualTo((int)Protocol.ASK_ALL_CONNECTED));
            Assert.That(parsedPacket.json, Is.Not.Null);
            Assert.That(string.IsNullOrEmpty(parsedPacket.json), Is.False);

            ProtocolObject.AskAllConnected reparsedAsk = PacketUtilities.GetProtocolObject<ProtocolObject.AskAllConnected>(parsedPacket);

            Assert.That(reparsedAsk, Is.Not.Null);
        }
    }
}
