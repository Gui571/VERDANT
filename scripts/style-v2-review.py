"""Compõe evidências com PNGs exportados pelo Forge; não desenha assets."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path('docs/arte/estilo-v2');src=root/'m1'
font='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
def text(d,xy,t,size=19,fill='#b5c2ce'):d.text(xy,t,font=ImageFont.truetype(font,size),fill=fill)
def page(title,sub):
 im=Image.new('RGB',(1280,720),'#151d2a');d=ImageDraw.Draw(im)
 text(d,(42,24),'VERDANT  /  FORGE V2  /  ESTUDOS DE MATERIAIS',15,'#d9b977')
 text(d,(42,66),title,36,'#f0eadb');text(d,(42,117),sub,18)
 d.line((42,660,1238,660),fill='#3d4857');text(d,(42,679),'M1 • formas atuais • materiais e luz gerados em C#',14);return im,d
def blit(im,name,x,y,z):
 p=Image.open(src/(name+'.png')).convert('RGBA');p=p.resize((p.width*z,p.height*z),Image.Resampling.NEAREST);im.paste(p,(x,y),p)
im,d=page('Um desenho. Materiais que conversam.','Silhueta → materiais → luz superior esquerda • cinco tons fixos por material')
for i,(file,label) in enumerate([('pipeline-Silhouette','MÁSCARA'),('pipeline-Materials','MATERIAIS'),('pipeline-Lit','LUZ E TOM')]):
 blit(im,file,100+i*240,167,6);text(d,(130+i*240,366),label,17,'#d9b977')
# Rampas do JSON gerado a partir dos bytes reais do C#, sem reimplementar OKLCH.
import json
ramps=json.load(open(src/'palettes.json'))
for row,name in enumerate(['Rabbit','Jade','Metal','Leather','Gold']):
 text(d,(896,175+row*38),name.upper(),14)
 for k,c in enumerate(ramps[name]):d.rectangle((995+k*43,175+row*38,1035+k*43,199+row*38),fill='#'+c)
for team,label in enumerate(['Rosa','Brasa','Lima','Jade','Azul','Violeta']):
 blit(im,'team-'+str(team),77+team*195,433,4);text(d,(105+team*195,573),label,16)
im.save(root/'m1-cor-luz.png')
im,d=page('O elenco, antes e depois.','Em cima: paleta atual. Embaixo: opção de materiais v2. Mesmas máscaras, equipe azul.')
for i,name in enumerate(['bram','nix','rowan','iria','boro','suri','mira']):
 x=50+i*173
 blit(im,name+'-v1',x,180,4);blit(im,name+'-v2',x,410,4);text(d,(x+30,560),name.capitalize(),17)
im.save(root/'m1-antes-depois.png')
im,d=page('Cor comum, anatomias preservadas.','Nove famílias • primeira linha: v1 • segunda linha: v2 • mesmos elementos e receitas')
for i,name in enumerate(['Slime','Beast','Golem','Dragon','Arthropod','Winged','Serpent','Aquatic','Plant']):
 x=43+i*135
 blit(im,'monster-'+name+'-v1',x,185,3);blit(im,'monster-'+name+'-v2',x,410,3);text(d,(x,535),name,13)
im.save(root/'m1-monstros.png')
for z in [1,2,3]:
 im=Image.new('RGB',(7*40*z,88*z),'#151d2a');d=ImageDraw.Draw(im);d.rectangle((0,44*z,im.width,im.height),fill='#eee6d1')
 for i,name in enumerate(['bram','nix','rowan','iria','boro','suri','mira']):
  blit(im,name+'-v2',(i*40+4)*z,4*z,z);blit(im,name+'-v2',(i*40+4)*z,48*z,z)
 im.save(root/f'm1-leitura-{z}x.png')
