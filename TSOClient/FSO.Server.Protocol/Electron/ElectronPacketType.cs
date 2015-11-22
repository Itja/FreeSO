﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Electron
{
    public enum ElectronPacketType
    {
        CreateASimResponse,
        PurchaseLotRequest,
        PurchaseLotResponse,
        InstantMessage,
        FindLotRequest,
        FindLotResponse,
        Unknown
    }

    public static class ElectronPacketTypeUtils
    {
        public static ElectronPacketType FromPacketCode(ushort code)
        {
            switch (code)
            {
                case 0x0001:
                    return ElectronPacketType.CreateASimResponse;
                case 0x0002:
                    return ElectronPacketType.PurchaseLotRequest;
                case 0x0003:
                    return ElectronPacketType.PurchaseLotResponse;
                case 0x0004:
                    return ElectronPacketType.InstantMessage;
                case 0x0005:
                    return ElectronPacketType.FindLotRequest;
                case 0x0006:
                    return ElectronPacketType.FindLotResponse;
                default:
                    return ElectronPacketType.Unknown;
            }
        }

        public static ushort GetPacketCode(this ElectronPacketType type)
        {
            switch (type)
            {
                case ElectronPacketType.CreateASimResponse:
                    return 0x0001;
                case ElectronPacketType.PurchaseLotRequest:
                    return 0x0002;
                case ElectronPacketType.PurchaseLotResponse:
                    return 0x0003;
                case ElectronPacketType.InstantMessage:
                    return 0x0004;
                case ElectronPacketType.FindLotRequest:
                    return 0x0005;
                case ElectronPacketType.FindLotResponse:
                    return 0x0006;
            }

            return 0xFFFF;
        }
    }
}
