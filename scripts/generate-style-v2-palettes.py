"""Gera tabelas sRGB de 8 bits offline; o jogo nunca calcula OKLCH.
Fórmula OKLab publicada por Björn Ottosson. Gamut mapping reduz croma.
"""
import math
from pathlib import Path
profiles = {
 'Skin': (65,.075), 'Fur': (65,.06), 'Metal': (255,.035), 'Wood':(65,.065),
 'Leather':(45,.055), 'Stone':(260,.018), 'Leaf':(135,.075), 'Canvas':(85,.025),
 'Gold':(85,.105), 'Bone':(95,.018), 'Accent':(45,.1),
 'Rose':(15,.12), 'Ember':(50,.12), 'Lime':(135,.12), 'Jade':(180,.1), 'Azure':(250,.12), 'Violet':(300,.12),
 'Duck':(95,.095), 'Rabbit':(90,.009), 'Human':(65,.075), 'Turtle':(115,.065),
 'Fox':(50,.105), 'Owl':(85,.027), 'Mouse':(45,.023), 'Cat':(260,.012),
 'Frog':(135,.09), 'Elf':(120,.07), 'Bear':(60,.07),
 'Amber':(65,.12), 'Ivory':(85,.028), 'Jewel':(335,.08),
 'Fire':(40,.12), 'Ice':(220,.07), 'Plant':(135,.09), 'Shadow':(300,.065), 'Poison':(110,.1),
 'Cornflower':(267,.145)
}
def linear(L,C,h):
 a=C*math.cos(math.radians(h));b=C*math.sin(math.radians(h))
 l=(L+.3963377774*a+.2158037573*b)**3
 m=(L-.1055613458*a-.0638541728*b)**3
 s=(L-.0894841775*a-1.2914855480*b)**3
 return (4.0767416621*l-3.3077115913*m+.2309699292*s,-1.2684380046*l+2.6097574011*m-.3413193965*s,-.0041960863*l-.7034186147*m+1.7076147010*s)
def color(L,C,h):
 for _ in range(80):
  rgb=linear(L,C,h)
  if all(0<=v<=1 for v in rgb):break
  C*=.94
 def q(v):return int(math.floor(max(0,min(1,12.92*v if v<=.0031308 else 1.055*v**(1/2.4)-.055))*255+.5))
 return ''.join(f'{q(v):02X}' for v in rgb)
def hue(h,target,t):return (h+((target-h+180)%360-180)*t)%360
rows=[]
for name,(h,c) in profiles.items():
 levels = {
  'Rabbit': [.48,.66,.88,.94,.99], 'Human':[.37,.55,.78,.87,.95], 'Duck':[.36,.53,.78,.87,.95],
  'Owl':[.42,.61,.84,.91,.97], 'Ivory':[.42,.61,.84,.91,.97], 'Bone':[.42,.61,.84,.91,.97],
  'Fox':[.3,.46,.7,.82,.93], 'Mouse':[.3,.43,.64,.77,.9], 'Cat':[.33,.49,.76,.87,.95],
  'Cornflower':[.30,.46,.69,.83,.96]
 }.get(name,[.31,.46,.67,.81,.94])
 if name in ['Rose','Ember','Lime','Jade','Azure','Violet']: levels=[.28,.4,.60,.77,.94]
 ramp=[color(L,c*k,hue(h,target,t)) for L,(k,target,t) in zip(levels,[(.65,270,.28),(.85,270,.12),(1,h,0),(.8,75,.08),(.45,75,.2)])]
 rows.append(f'    public static readonly Ramp5 {name} = new('+', '.join('0x'+v for v in ramp)+');')
out='''// Gerado por scripts/generate-style-v2-palettes.py; não editar os bytes manualmente.
namespace Verdant.Forge.StyleV2;
public static class MaterialRamps
{
'''+ '\n'.join(rows)+'''
    public static readonly IReadOnlyList<Ramp5> Teams = Array.AsReadOnly(new[] { Rose, Ember, Lime, Jade, Azure, Violet });
}
'''
Path('pixel_demo/Forge/StyleV2/MaterialRamps.g.cs').write_text(out)
