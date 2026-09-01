$src = @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static class HostDetailResources {
  static readonly Color T = Color.Transparent;
  static readonly Color O = Color.FromArgb(255, 5, 8, 18);
  static readonly Color N = Color.FromArgb(255, 12, 20, 36);
  static readonly Color S = Color.FromArgb(255, 31, 53, 70);
  static readonly Color L = Color.FromArgb(255, 76, 119, 132);
  static readonly Color W = Color.FromArgb(255, 202, 230, 222);
  static readonly Color P = Color.FromArgb(255, 124, 61, 196);
  static readonly Color V = Color.FromArgb(255, 211, 93, 255);
  static readonly Color R = Color.FromArgb(255, 205, 51, 59);
  static readonly Color Y = Color.FromArgb(255, 246, 183, 50);
  static readonly Color C = Color.FromArgb(255, 55, 208, 222);
  static readonly Color B = Color.FromArgb(255, 66, 117, 205);
  static readonly Color G = Color.FromArgb(255, 77, 190, 91);

  static Bitmap New(int w,int h) { var b=new Bitmap(w,h,PixelFormat.Format32bppArgb); using(var g=Graphics.FromImage(b)) g.Clear(T); return b; }
  static void Rect(Graphics g, Color c, int x,int y,int w,int h) { using(var b=new SolidBrush(c)) g.FillRectangle(b,x,y,w,h); }
  static void Poly(Graphics g, Color c, params int[] xy) { var p=new Point[xy.Length/2]; for(int i=0;i<p.Length;i++) p[i]=new Point(xy[i*2],xy[i*2+1]); using(var b=new SolidBrush(c)) g.FillPolygon(b,p); }
  static void Line(Graphics g, Color c, int x1,int y1,int x2,int y2,int w=1) { using(var p=new Pen(c,w)) { p.StartCap=System.Drawing.Drawing2D.LineCap.Square; p.EndCap=p.StartCap; g.DrawLine(p,x1,y1,x2,y2); } }
  static void Save(Bitmap b,string path) { b.Save(path,ImageFormat.Png); b.Dispose(); }
  static Graphics Begin(Bitmap b) { var g=Graphics.FromImage(b); g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.None; g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor; g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.None; return g; }
  static void Spark(Graphics g,int x,int y,Color c) { Rect(g,c,x-1,y-4,3,9); Rect(g,c,x-4,y-1,9,3); Rect(g,W,x,y-2,1,5); }
  static void Droplet(Graphics g,int x,int y,int s,Color c) { Poly(g,O,x,y-s-2,x-s-1,y+1,x-s+1,y+s,x,y+s+2,x+s-1,y+s,x+s+1,y+1); Poly(g,c,x,y-s,x-s+1,y+2,x,y+s,x+s-1,y+2); Rect(g,Color.FromArgb(255,255,124,118),x-1,y-s+2,2,3); }
  static void ArrowDown(Graphics g,int x,int y,Color c) { Rect(g,O,x-3,y-8,7,10); Poly(g,O,x-8,y,x+8,y,x,y+9); Rect(g,c,x-1,y-7,3,9); Poly(g,c,x-5,y+1,x+5,y+1,x,y+6); }

  static void IconBase(Graphics g) { Rect(g,S,8,39,28,2); Rect(g,N,11,41,22,1); }
  static void ShieldReversal(string p) { var b=New(44,44); using(var g=Begin(b)){ IconBase(g); Poly(g,O,22,4,35,9,33,25,22,36,11,25,9,9); Poly(g,B,22,7,32,11,30,23,22,32,14,23,12,11); Poly(g,C,22,9,29,12,27,20,22,25,17,20,15,12); Rect(g,W,21,10,3,14); Rect(g,W,17,17,11,3); Poly(g,O,29,6,38,6,38,15,35,15,35,10,29,10); Poly(g,Y,30,7,37,7,37,14,35,14,35,10,30,10); } Save(b,p); }
  static void Ricochet(string p) { var b=New(44,44); using(var g=Begin(b)){ IconBase(g); Line(g,O,10,33,32,11,7); Line(g,W,10,31,30,11,3); Rect(g,Y,8,31,7,5); Rect(g,R,30,8,5,8); Poly(g,C,29,24,37,20,39,23,32,27); Line(g,C,33,23,39,14,2); Spark(g,38,12,Y); } Save(b,p); }
  static void LeechScythe(string p) { var b=New(44,44); using(var g=Begin(b)){ IconBase(g); Line(g,O,14,36,29,9,6); Line(g,W,15,35,29,10,2); Poly(g,O,27,5,40,10,34,20,30,18,34,11,27,9); Poly(g,L,29,7,37,10,33,16,31,15,34,11,29,9); Droplet(g,8,18,4,R); Line(g,R,12,19,20,23,2); Poly(g,R,18,19,25,24,17,27); } Save(b,p); }
  static void Flame(string p) { var b=New(44,44); using(var g=Begin(b)){ IconBase(g); Poly(g,O,22,3,29,13,34,10,36,23,30,36,14,36,8,25,13,14,16,19); Poly(g,R,22,7,27,17,31,15,32,25,27,33,16,33,12,25,17,17,18,23); Poly(g,Y,22,15,27,22,25,31,18,31,16,25); Rect(g,W,21,25,3,5); } Save(b,p); }
  static void WeakBlue(string p,bool snow) { var b=New(44,44); using(var g=Begin(b)){ IconBase(g); Color q=snow?C:B; if(snow){ Line(g,O,22,5,22,24,5); Line(g,O,12,10,32,22,5); Line(g,O,32,10,12,22,5); Line(g,q,22,6,22,23,2); Line(g,q,13,11,31,21,2); Line(g,q,31,11,13,21,2); } else { Poly(g,O,22,4,35,11,33,26,22,34,11,26,9,11); Poly(g,q,22,7,31,12,29,23,22,29,15,23,13,12); Rect(g,C,20,11,4,12); } ArrowDown(g,22,33,snow?W:C); } Save(b,p); }
  static void HotMagazine(string p) { var b=New(44,44); using(var g=Begin(b)){ IconBase(g); Rect(g,O,9,7,23,28); Rect(g,S,12,9,17,23); Rect(g,L,14,11,13,5); for(int i=0;i<4;i++) Rect(g,Y,14+i*3,20,2,7); Rect(g,R,25,20,2,7); Poly(g,O,30,5,39,11,34,20,29,14); Poly(g,Y,31,7,36,11,33,16,31,13); Spark(g,36,7,W); } Save(b,p); }
  static void Fangs(string p) { var b=New(44,44); using(var g=Begin(b)){ IconBase(g); Poly(g,O,7,9,19,8,20,29,14,37,10,26); Poly(g,O,37,9,25,8,24,29,30,37,34,26); Poly(g,W,10,11,17,11,17,27,14,33,12,25); Poly(g,W,34,11,27,11,27,27,30,33,32,25); Droplet(g,22,24,4,R); } Save(b,p); }
  static void SpringShot(string p) { var b=New(44,44); using(var g=Begin(b)){ IconBase(g); Rect(g,O,8,26,18,10); Rect(g,G,10,28,14,5); Line(g,O,21,29,29,22,5); Line(g,Y,21,29,29,22,2); Line(g,O,28,21,35,14,5); Line(g,Y,28,21,35,14,2); Poly(g,O,31,10,41,12,37,20); Poly(g,R,33,12,39,13,36,17); Line(g,C,7,18,17,18,2); Line(g,C,10,14,18,14,2); } Save(b,p); }
  static void RuneWeak(string p) { var b=New(44,44); using(var g=Begin(b)){ IconBase(g); Poly(g,O,22,3,35,12,31,28,22,35,13,28,9,12); Poly(g,P,22,7,31,13,28,25,22,30,16,25,13,13); Rect(g,V,20,11,4,13); Rect(g,W,18,17,8,3); Spark(g,10,8,C); ArrowDown(g,34,30,V); } Save(b,p); }
  static void Circuit(string p) { var b=New(44,44); using(var g=Begin(b)){ IconBase(g); Rect(g,O,8,8,28,27); Rect(g,S,11,11,22,21); Rect(g,C,15,14,14,11); Rect(g,N,18,17,8,5); for(int i=0;i<4;i++){Rect(g,L,5,12+i*5,5,2);Rect(g,L,34,12+i*5,5,2);} Line(g,G,20,31,20,38,2); Line(g,G,24,31,24,38,2); Rect(g,Y,18,36,8,3); Spark(g,31,8,V); } Save(b,p); }

  static void Badge(string p, Color a, Color z) { var b=New(88,24); using(var g=Begin(b)){ Poly(g,O,3,0,85,0,88,3,88,20,84,24,4,24,0,20,0,4); Poly(g,a,4,2,84,2,86,4,86,19,83,22,5,22,2,19,2,5); Rect(g,N,5,5,78,14); Rect(g,z,7,5,74,2); Rect(g,a,7,19,74,1); Rect(g,z,4,8,2,8); Rect(g,z,82,8,2,8); } Save(b,p); }
  static void PassiveCard(string p) { var b=New(263,124); using(var g=Begin(b)){ Poly(g,O,5,0,258,0,263,5,263,119,258,124,5,124,0,119,0,5); Poly(g,S,5,2,258,2,261,5,261,119,258,122,5,122,2,119,2,5); Rect(g,N,5,5,253,114); Rect(g,Color.FromArgb(255,20,22,45),7,7,249,110); Rect(g,P,7,7,249,2); Rect(g,L,7,34,249,1); Rect(g,S,7,100,249,1); Rect(g,V,12,14,3,12); Rect(g,C,18,14,1,12); Rect(g,P,244,14,3,12); Rect(g,C,240,14,1,12); } Save(b,p); }
  static void BarBg(string p) { var b=New(239,10); using(var g=Begin(b)){ Rect(g,O,0,1,239,8); Rect(g,S,1,2,237,6); Rect(g,N,2,3,235,4); Rect(g,L,2,3,235,1); } Save(b,p); }
  static void BarFill(string p) { var b=New(239,10); using(var g=Begin(b)){ Rect(g,P,0,2,239,6); Rect(g,V,0,2,239,2); Rect(g,Color.FromArgb(255,95,43,164),0,6,239,2); for(int x=6;x<239;x+=18) Rect(g,Color.FromArgb(255,231,145,255),x,3,2,2); } Save(b,p); }

  public static void Build(string root) {
    Directory.CreateDirectory(root);
    ShieldReversal(Path.Combine(root,"passiveicon_amazon_elite.png"));
    Ricochet(Path.Combine(root,"passiveicon_baseball.png"));
    LeechScythe(Path.Combine(root,"passiveicon_death.png"));
    Flame(Path.Combine(root,"passiveicon_salamander.png"));
    WeakBlue(Path.Combine(root,"passiveicon_dragon_blue.png"),false);
    WeakBlue(Path.Combine(root,"passiveicon_snowwoman.png"),true);
    HotMagazine(Path.Combine(root,"passiveicon_hopper_smg.png"));
    Fangs(Path.Combine(root,"passiveicon_vampire.png"));
    SpringShot(Path.Combine(root,"passiveicon_hopper.png"));
    RuneWeak(Path.Combine(root,"passiveicon_white_wizard.png"));
    Circuit(Path.Combine(root,"passiveicon_robot.png"));
    Badge(Path.Combine(root,"jobbadge_melee.png"),Color.FromArgb(255,91,31,38),R);
    Badge(Path.Combine(root,"jobbadge_midrange.png"),Color.FromArgb(255,92,61,22),Y);
    Badge(Path.Combine(root,"jobbadge_ranged.png"),Color.FromArgb(255,25,50,91),B);
    Badge(Path.Combine(root,"jobbadge_piercing.png"),Color.FromArgb(255,18,72,82),C);
    PassiveCard(Path.Combine(root,"passiveskillcard.png"));
    BarBg(Path.Combine(root,"shardbarbg.png"));
    BarFill(Path.Combine(root,"shardbarfill.png"));
  }
}
'@
Add-Type -TypeDefinition $src -ReferencedAssemblies System.Drawing
[HostDetailResources]::Build('C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\in')
