using System;

namespace SkillWallet {
    // Continuous coordinates in card units; small fixed integration steps avoid frame-rate dependence.
    public class CarouselMotion {
        public double Position {get;private set;}
        public double Velocity {get;private set;}
        public double Target {get;private set;}
        public int Count {get;private set;}
        public bool IsDragging {get;private set;}
        double origin,lastDragTime,lastDragPosition,lastSampleTime;
        public bool Moving {get{return Math.Abs(Position-Target)>.0005||Math.Abs(Velocity)>.005;}}
        public double Clamp(double x){return Math.Max(0,Math.Min(Math.Max(0,Count-1),x));}
        public void Reset(int count,double position) {Count=count;Position=Target=Clamp(position);Velocity=0;IsDragging=false;}
        public void MoveTo(double target) {Target=Clamp(target);IsDragging=false;}
        public void BeginDrag(double now) {origin=Position;Target=Position;Velocity=0;IsDragging=true;lastDragTime=lastSampleTime=now;lastDragPosition=Position;}
        static double Rubber(double distance){return .65*(1-1/(1+Math.Abs(distance)/.65));}
        public void Drag(double cardDelta,double now) {
            double raw=origin+cardDelta;
            Position=raw<0?-Rubber(raw):raw>Count-1?Math.Max(0,Count-1)+Rubber(raw-(Count-1)):raw;
            double dt=now-lastSampleTime;
            if(dt>=.008) {
                double instant=(Position-lastDragPosition)/dt;
                Velocity=Velocity*.35+Math.Max(-14,Math.Min(14,instant))*.65;
                lastSampleTime=now;lastDragPosition=Position;
            }
            lastDragTime=now;
        }
        public void Release(double now,bool inertia) {
            IsDragging=false;
            if(!inertia||now-lastDragTime>.11)Velocity=0;
            double projected=Position+Math.Max(-3,Math.Min(3,Velocity*.18));
            Target=Clamp(Math.Round(projected,MidpointRounding.AwayFromZero));
            if(Position<0||Position>Count-1)Velocity=0;
        }
        public void Step(double elapsed) {
            if(IsDragging)return;
            double remaining=Math.Min(.1,Math.Max(0,elapsed));
            while(remaining>0) {
                double dt=Math.Min(1.0/240,remaining);
                Velocity+=(200*(Target-Position)-25*Velocity)*dt;Position+=Velocity*dt;remaining-=dt;
            }
            if(!Moving){Position=Target;Velocity=0;}
        }
    }
}
