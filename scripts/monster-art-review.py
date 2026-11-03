# Composição de evidências; todos os pixels de criaturas vêm do exportador C# Forge.
from pathlib import Path
from PIL import Image,ImageDraw
families=['slime','beast','golem','dragon','arthropod','winged','serpent','aquatic','plant']
for fam in families:
 out=Image.new('RGBA',(2048,10*256),(28,32,49,255))
 for row,m in enumerate(['idle','walk','run','attack','hurt','death','special','cast','entry']):
  im=Image.open(f'docs/monsters/{fam}-{m}.png');
  zoom=im.crop((0,0,min(im.width,256),32)).resize((min(im.width,256)*8,256),Image.Resampling.NEAREST);out.alpha_composite(zoom,(0,(row+(1 if row>0 else 0))*256))
  if m=='idle':out.alpha_composite(im.crop((256,0,512,32)).resize((2048,256),Image.Resampling.NEAREST),(0,256))
  frames=[]
  for f in range(im.width//32):
   cell=Image.new('RGBA',(256,256),(28,32,49,255));cell.alpha_composite(im.crop((f*32,0,(f+1)*32,32)).resize((256,256),Image.Resampling.NEAREST));frames.append(cell.convert('RGB'))
  frames[0].save(f'docs/arte/depois/{fam}-{m}.gif',save_all=True,append_images=frames[1:],duration=83 if m in ['run','death'] else 125,loop=0)
 out.save(f'docs/arte/depois/{fam}-8x.png')
 matrix=Image.open(f'docs/monsters/{fam}-elements-ranks.png');bg=Image.new('RGBA',matrix.size,(28,32,49,255));bg.alpha_composite(matrix);bg.resize((matrix.width*8,matrix.height*8),Image.Resampling.NEAREST).save(f'docs/arte/depois/{fam}-elements-ranks-8x.png')
board=Image.new('RGB',(9*256,512),(28,32,49))
for i,fam in enumerate(families):
 for row,folder in enumerate(['docs/arte/antes','docs/monsters']):
  im=Image.open(f'{folder}/{fam}-idle.png').crop((0,0,32,32)).resize((256,256),Image.Resampling.NEAREST);board.paste(im,(i*256,row*256),im)
board.save('docs/arte/antes-depois-monstros.png')
for scale in [1,2,3]:
 out=Image.new('RGB',(9*40*scale,40*scale),(28,32,49))
 for i,fam in enumerate(families):
  im=Image.open(f'docs/monsters/{fam}-idle.png').crop((0,0,32,32)).resize((32*scale,32*scale),Image.Resampling.NEAREST);out.paste(im,(i*40*scale,0),im)
 out.save(f'docs/arte/leitura-{scale}x.png')

# Mantém links antigos apontando para pranchas regeneradas, não para a auditoria anterior.
for motion in ['idle','walk','run','attack','hurt','death','special','cast','entry']:
 rows=[Image.open(f'docs/monsters/{f}-{motion}-review.png') for f in families]
 board=Image.new('RGB',(max(i.width for i in rows),sum(i.height for i in rows)),(28,32,49))
 y=0
 for im in rows:board.paste(im,(0,y));y+=im.height
 board.save(f'docs/monsters/all-{motion}-review.png')
