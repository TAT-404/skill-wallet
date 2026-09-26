using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace SkillWallet {
    // Keep a tiny PCM queue open only while sound is enabled. No startup sound.
    public sealed class SoftSound : IDisposable {
        [StructLayout(LayoutKind.Sequential,Pack=2)] struct Format {public ushort tag,channels;public uint rate,bytes;public ushort align,bits,extra;}
        [StructLayout(LayoutKind.Sequential)] struct Header {public IntPtr data;public uint length,recorded;public UIntPtr user;public uint flags,loops;public IntPtr next;public UIntPtr reserved;}
        [DllImport("winmm.dll")] static extern uint waveOutOpen(out IntPtr handle,uint device,ref Format format,IntPtr callback,IntPtr instance,uint flags);
        [DllImport("winmm.dll")] static extern uint waveOutPrepareHeader(IntPtr handle,IntPtr header,uint size);
        [DllImport("winmm.dll")] static extern uint waveOutUnprepareHeader(IntPtr handle,IntPtr header,uint size);
        [DllImport("winmm.dll")] static extern uint waveOutWrite(IntPtr handle,IntPtr header,uint size);
        [DllImport("winmm.dll")] static extern uint waveOutReset(IntPtr handle);
        [DllImport("winmm.dll")] static extern uint waveOutClose(IntPtr handle);
        readonly Thread thread;readonly string logPath;volatile bool stopping;int pending;double last=-1;DateTime lastLog=DateTime.MinValue;
        public volatile bool Ready;public int WrittenBlocks;
        public SoftSound(string path){logPath=path;thread=new Thread(Run){IsBackground=true,Name="SkillWallet audio"};thread.Start();}
        static readonly short[] cueSamples=LoadCue();
        static short[] LoadCue(){using(var stream=typeof(SoftSound).Assembly.GetManifestResourceStream("SkillWallet.slide-tick.pcm")){if(stream==null||stream.Length<2||stream.Length>17640||stream.Length%2!=0)throw new InvalidDataException("Missing slide cue.");var samples=new short[(int)stream.Length/2];using(var reader=new BinaryReader(stream))for(int i=0;i<samples.Length;i++)samples[i]=reader.ReadInt16();return samples;}}
        static short Sample(int i){return cueSamples[i];}
        public static byte[] CreateWave(){using(var memory=new MemoryStream())using(var writer=new BinaryWriter(memory)){int length=cueSamples.Length*2;writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+length);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(44100);writer.Write(88200);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(length);for(int i=0;i<cueSamples.Length;i++)writer.Write(Sample(i));return memory.ToArray();}}
        public bool TryPlay(double now){if(stopping||now-last<.065)return false;last=now;Interlocked.Exchange(ref pending,1);return true;}
        public void Stop(){Interlocked.Exchange(ref pending,0);}
        void Check(uint code,string operation){if(code!=0)throw new InvalidOperationException(operation+" code="+code);}
        void Log(Exception ex){if((DateTime.UtcNow-lastLog).TotalSeconds<5)return;lastLog=DateTime.UtcNow;try{Directory.CreateDirectory(Path.GetDirectoryName(logPath));if(File.Exists(logPath)&&new FileInfo(logPath).Length>64000)File.WriteAllText(logPath,"");File.AppendAllText(logPath,DateTime.UtcNow.ToString("o")+" "+ex.Message+Environment.NewLine);}catch{}}
        void Run() {
            while(!stopping){IntPtr device=IntPtr.Zero;var data=new IntPtr[3];var headers=new IntPtr[3];var prepared=new bool[3];uint size=(uint)Marshal.SizeOf(typeof(Header));
                try {
                    var format=new Format {tag=1,channels=1,rate=44100,bytes=88200,align=2,bits=16};Check(waveOutOpen(out device,UInt32.MaxValue,ref format,IntPtr.Zero,IntPtr.Zero,0),"open");
                    const int samples=882;var block=new short[samples];
                    for(int i=0;i<3;i++){data[i]=Marshal.AllocHGlobal(samples*2);Marshal.Copy(block,0,data[i],samples);headers[i]=Marshal.AllocHGlobal((int)size);Marshal.StructureToPtr(new Header {data=data[i],length=samples*2},headers[i],false);Check(waveOutPrepareHeader(device,headers[i],size),"prepare");prepared[i]=true;Check(waveOutWrite(device,headers[i],size),"prime");}Ready=true;
                    int cursor=0,cue=cueSamples.Length;DateTime opened=DateTime.UtcNow;
                    while(!stopping){var header=(Header)Marshal.PtrToStructure(headers[cursor],typeof(Header));if((header.flags&1)==0){Thread.Sleep(4);continue;}
                        if((DateTime.UtcNow-opened).TotalMilliseconds>180&&Interlocked.Exchange(ref pending,0)!=0)cue=0;
                        for(int j=0;j<samples;j++)block[j]=cue<cueSamples.Length?Sample(cue++):(short)0;
                        Marshal.Copy(block,0,data[cursor],samples);Check(waveOutWrite(device,headers[cursor],size),"write");Interlocked.Increment(ref WrittenBlocks);cursor=(cursor+1)%3;
                    }
                } catch(Exception ex){Log(ex);} finally {
                    Ready=false;if(device!=IntPtr.Zero)waveOutReset(device);
                    for(int i=0;i<3;i++){if(prepared[i])waveOutUnprepareHeader(device,headers[i],size);if(headers[i]!=IntPtr.Zero)Marshal.FreeHGlobal(headers[i]);if(data[i]!=IntPtr.Zero)Marshal.FreeHGlobal(data[i]);}if(device!=IntPtr.Zero)waveOutClose(device);
                }
                for(int n=0;n<100&&!stopping;n++)Thread.Sleep(20);
            }
        }
        public void Dispose(){stopping=true;Interlocked.Exchange(ref pending,0);}
    }
}
