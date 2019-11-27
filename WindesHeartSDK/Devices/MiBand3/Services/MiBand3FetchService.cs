﻿using Plugin.BluetoothLE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using WindesHeartSdk.Model;
using WindesHeartSDK.Devices.MiBand3Device.Helpers;
using WindesHeartSDK.Devices.MiBand3Device.Models;
using WindesHeartSDK.Devices.MiBand3Device.Resources;
using WindesHeartSDK.Helpers;
using static WindesHeartSDK.Helpers.ConversionHelper;

namespace WindesHeartSDK.Devices.MiBand3Device.Services
{
    class MiBand3FetchService
    {
        private readonly MiBand3 MiBand3;
        private readonly List<ActivitySample> Samples = new List<ActivitySample>();

        private DateTime firstTimeStamp;
        private DateTime lastTimeStamp;
        private int pkg = 0;

        private IDisposable CharUnknownSub;
        private IDisposable CharActivitySub;


        public MiBand3FetchService(MiBand3 device)
        {
            MiBand3 = device;
        }

        public void InitiateFetching(DateTime date)
        {
            CharActivitySub?.Dispose();
            CharUnknownSub?.Dispose();
            CharUnknownSub = MiBand3.GetCharacteristic(MiBand3Resource.GuidUnknownCharacteristic4).RegisterAndNotify().Subscribe(handleUnknownChar);
            CharActivitySub  = MiBand3.GetCharacteristic(MiBand3Resource.GuidCharacteristic5ActivityData).RegisterAndNotify().Subscribe(handleActivityChar);
            WriteDateBytes(date);
        }

        private async void WriteDateBytes(DateTime date)
        {
            byte[] Timebytes = GetTimeBytes(date, TimeUnit.Minutes);
            byte[] Fetchbytes = new byte[10] { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0 };

            Buffer.BlockCopy(Timebytes, 0, Fetchbytes, 2, 8);

            await MiBand3.GetCharacteristic(MiBand3Resource.GuidUnknownCharacteristic4).WriteWithoutResponse(Fetchbytes);
        }

        public async void handleUnknownChar(CharacteristicGattResult result)
        {
            Console.WriteLine("handleUnknownChar");
            byte[] responseByte = new byte[3];
            Buffer.BlockCopy(result.Data, 0, responseByte, 0, 3);

            Console.WriteLine("responseByte: " + responseByte[0].ToString() + " - " + responseByte[1].ToString() + " - " + responseByte[2].ToString());
            if (result.Data.Length > 3)
            {
                Console.WriteLine("Expected Samples: " + result.Data[3].ToString() + " - " + result.Data[4].ToString() + " - " + result.Data[5].ToString());
            }

            if(responseByte.SequenceEqual(new byte[3] { 0x10, 0x01, 0x01 }))
            {
                Console.WriteLine("First If");

                // Get the timestamp of the first sample
                byte[] DateTimeBytes = new byte[8];
                Buffer.BlockCopy(result.Data, 7, DateTimeBytes, 0, 8);
                firstTimeStamp = RawBytesToCalendar(DateTimeBytes);

                Console.WriteLine("Fetching data from: " + firstTimeStamp.ToString());
                CharActivitySub = MiBand3.GetCharacteristic(MiBand3Resource.GuidCharacteristic5ActivityData).RegisterAndNotify().Subscribe(handleActivityChar);
                await MiBand3.GetCharacteristic(MiBand3Resource.GuidUnknownCharacteristic4).WriteWithoutResponse(new byte[] { 0x02 });
                Console.WriteLine("Done writing 0x02");


            }
            else if(responseByte.SequenceEqual(new byte[3] { 0x10, 0x02, 0x01 }))
            {
                Console.WriteLine("Done Fetching: " + Samples.Count + " Samples");
                CharActivitySub?.Dispose();
                CharUnknownSub?.Dispose();
                foreach(ActivitySample sample in Samples)
                {
                    Console.WriteLine(sample.ToString());
                }
            }
            else
            {
                Console.WriteLine("Error while Fetching");
                // Error while fetching
                CharActivitySub?.Dispose();
                CharUnknownSub?.Dispose();
            }
        }

        private void handleActivityChar(CharacteristicGattResult result)
        {
            Console.WriteLine("HandleActivityChar");


            if (result.Data.Length % 4 != 1)
            {
                if (lastTimeStamp > DateTime.Now.AddMinutes(-1))
                {
                    Console.WriteLine("Done Fetching: " + Samples.Count + " Samples");
                }
                Console.WriteLine("Need More fetching");
                InitiateFetching(lastTimeStamp.AddMinutes(1));
            }
            else
            {
                Console.WriteLine("ElseStatement");
                var LocalPkg = pkg; // ??
                pkg++;
                var i = 1;
                while (i < result.Data.Length)
                {
                    int timeIndex = (LocalPkg) * 4 + (i - 1) / 4;
                    var timeStamp = firstTimeStamp.AddMinutes(timeIndex);
                    lastTimeStamp = timeStamp; //This doesn't seem right


                    foreach (byte b in result.Data)
                    {
                        Console.WriteLine(b);
                    }

                    var category = result.Data[i]; //ToUint16(new byte[] { result.Data[i], result.Data[i + 1] });
                    var intensity = result.Data[i + 1]; //ToUint16(new byte[] { result.Data[i], result.Data[i + 1] });
                    var steps = result.Data[i + 2] & 0xff;
                    var heartrate = result.Data[i + 3];

                    Samples.Add(new ActivitySample(timeStamp, category, intensity, steps, heartrate));
                    Console.WriteLine("Added Sample: Total = " + Samples.Count);

                    i += 4;

                    var d = DateTime.Now.AddMinutes(-1);
                    d.AddSeconds(-d.Second);
                    d.AddMilliseconds(-d.Millisecond);
                    

                    if (timeStamp == d)
                    {
                        Console.WriteLine("Done Fetching");
                        break;
                    }
                }
            }
        }
    }
}
