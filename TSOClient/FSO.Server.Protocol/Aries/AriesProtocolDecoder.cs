﻿using Mina.Filter.Codec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using Mina.Core.Session;
using NLog;
using FSO.Server.Protocol.Utils;
using FSO.Server.Protocol.Voltron;
using FSO.Server.Protocol.Aries.Packets;

namespace FSO.Server.Protocol.Aries
{
    public class AriesProtocolDecoder : CumulativeProtocolDecoder
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        protected override bool DoDecode(IoSession session, IoBuffer buffer, IProtocolDecoderOutput output)
        {
            if(buffer.Remaining < 12){
                return false;
            }

            /**
             * We expect aries, voltron or electron packets
             */
            buffer.Order = ByteOrder.LittleEndian;
            uint packetType = buffer.GetUInt32();
            uint timestamp = buffer.GetUInt32();
            uint payloadSize = buffer.GetUInt32();

            if (buffer.Remaining < payloadSize)
            {
                buffer.Skip(-12);
                /** Not all here yet **/
                return false;
            }

            LOG.Info("[ARIES] " + packetType + " (" + payloadSize + ")");

            if(packetType == 0)
            {
                while(payloadSize > 0)
                {
                    /** Voltron packet **/
                    buffer.Order = ByteOrder.BigEndian;
                    ushort voltronType = buffer.GetUInt16();
                    if(voltronType == VoltronPacketType.SetIgnoreListPDU.GetPacketCode())
                    {
                        int y = 22;
                    }
                    uint voltronPayloadSize = buffer.GetUInt32() - 6;

                    byte[] data = new byte[(int)voltronPayloadSize];
                    buffer.Get(data, 0, (int)voltronPayloadSize);

                    var packetClass = VoltronPackets.GetByPacketCode(voltronType);
                    if (packetClass != null)
                    {
                        IVoltronPacket packet = (IVoltronPacket)Activator.CreateInstance(packetClass);
                        LOG.Info("[VOLTRON-IN] " + packet.GetPacketType().ToString() + " (" + packet.ToString() + ")");
                        packet.Deserialize(IoBuffer.Wrap(data));
                        output.Write(packet);
                    }
                    else
                    {
                        LOG.Info("[VOLTRON-IN] " + VoltronPacketTypeUtils.FromPacketCode(voltronType) + " (" + voltronPayloadSize + ")");
                    }

                    payloadSize -= voltronPayloadSize + 6;
                }
            }
            else
            {
                var packetClass = AriesPackets.GetByPacketCode(packetType);
                if (packetClass != null)
                {
                    byte[] data = new byte[(int)payloadSize];
                    buffer.Get(data, 0, (int)payloadSize);

                    IAriesPacket packet = (IAriesPacket)Activator.CreateInstance(packetClass);
                    packet.Deserialize(IoBuffer.Wrap(data));
                    output.Write(packet);

                    payloadSize = 0;
                }
                else
                {
                    buffer.Skip((int)payloadSize);
                    payloadSize = 0;
                }
            }

            

            return true;
        }
    }
}
