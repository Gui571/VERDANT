"""Pranchas de revisão: os sprites são exclusivamente os PNGs do Forge C#."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path('docs/arte/compactos');src=root/'atlas'
font='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
names=['bram','nix','rowan','iria','boro','suri','mira']
def page(title,subtitle,h=720):
 im=Image.new('RGB',(1280,h),'#151e2a');d=ImageDraw.Draw(im)
 d.text((40,26),'VERDANT / ESTUDOS DO ELENCO',font=ImageFont.truetype(font,15),fill='#c8ac72')
 d.text((40,64),title,font=ImageFont.truetype(font,34),fill='#f0eade');d.text((40,112),subtitle,font=ImageFont.truetype(font,17),fill='#aebaca');return im,d
def put(im,p,x,y,z):
 a=Image.open(p).convert('RGBA');a=a.resize((a.width*z,a.height*z),Image.Resampling.NEAREST);im.paste(a,(x,y),a)
im,d=page('Cabeças grandes. Formas mais leves.','Desenho novo em código • cabeça dominante, corpo curto e equipamento separado')
for i,n in enumerate(names):
 x=45+i*174;put(im,src/(n+'.png'),x,235,4);d.text((x+25,380),n.capitalize(),font=ImageFont.truetype(font,18),fill='#c8ac72')
# Em 1×/2×/3×, preservando os pixels nativos.
for row,z in enumerate([1,2,3]):
 d.text((44,468+row*66),f'{z}×',font=ImageFont.truetype(font,16),fill='#aebaca')
 for i,n in enumerate(names):put(im,src/(n+'.png'),100+i*160,430+row*66,z)
im.save(root/'elenco.png')
im,d=page('A forma mudou, não só a paleta.','Em cima: entrega anterior. Embaixo: desenho compacto novo.')
for i,n in enumerate(names):
 x=45+i*174;put(im,Path('docs/arte/estilo-v2/m1')/(n+'-v2.png'),x,180,4);put(im,src/(n+'.png'),x,420,4);d.text((x+25,568),n.capitalize(),font=ImageFont.truetype(font,18),fill='#c8ac72')
im.save(root/'comparacao.png')
im,d=page('Mesma escala, espécies diferentes.','Onze espécies atuais • acessórios e anatomia construídos por receita')
for i,n in enumerate(['Duck','Rabbit','Human','Turtle','Fox','Owl','Mouse','Cat','Frog','Elf','Bear']):
 x=60+i%6*200;y=180+i//6*240;put(im,src/('species-'+n+'.png'),x,y,4);d.text((x+30,y+140),n,font=ImageFont.truetype(font,17),fill='#c8ac72')
im.save(root/'especies.png')
for n in names:
 sheet=Image.open(src/(n+'-Idle.png')).convert('RGBA');frames=[]
 for f in range(sheet.width//32):
  im=Image.new('RGBA',(256,256),'#151e2a');im.alpha_composite(sheet.crop((f*32,0,(f+1)*32,32)).resize((256,256),Image.Resampling.NEAREST));frames.append(im.convert('RGB'))
 frames[0].save(root/(n+'-idle.gif'),save_all=True,append_images=frames[1:],duration=125,loop=0)
# Todos os quadros do ataque, em 8×.
for n in names:
 p=Image.open(src/(n+'-Attack.png')).convert('RGBA');im=Image.new('RGBA',(p.width,p.height),'#151e2a');im.alpha_composite(p);im.resize((p.width*8,p.height*8),Image.Resampling.NEAREST).save(root/(n+'-ataque-8x.png'))
