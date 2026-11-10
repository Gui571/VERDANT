"""Compõe as folhas de apresentação do elenco v2 a partir dos sprites exportados por `PixelPreview cast <dir>`.
Uso: python3 scripts/compose-cast-sheets.py <dir-com-sprites> <dir-saida>
Só rotula e amplia em inteiros (vizinho mais próximo); nada de pintura própria."""
import sys, glob, os
from PIL import Image, ImageDraw, ImageFont
src, out = sys.argv[1], sys.argv[2]; os.makedirs(out, exist_ok=True)
BG=(21,24,41); AMBER=(226,190,120); TXT=(196,204,222)
try: F=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',13); FB=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',26); FS=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',15)
except Exception: F=FB=FS=ImageFont.load_default()
def load(n): return Image.open(os.path.join(src,n+'.png')).convert('RGBA')
def put(img,im,x,y,z): 
    im=im.resize((im.width*z,im.height*z),Image.NEAREST); img.paste(im,(x,y),im)
def head(d,kicker,title,sub,W):
    d.text((32,22),kicker,font=F,fill=AMBER); d.text((32,44),title,font=FB,fill=(245,243,236)); d.text((32,82),sub,font=FS,fill=TXT)
def grid(name,items,cols,title,sub,z=5,cell=None):
    cw=32*z+30; rows=(len(items)+cols-1)//cols; W=cols*cw+64; H=130+rows*(32*z+34)+40
    img=Image.new('RGB',(W,H),BG); d=ImageDraw.Draw(img); head(d,'VERDANT / ESTILO V2',title,sub,W)
    for i,(f,label) in enumerate(items):
        x=32+(i%cols)*cw; y=120+(i//cols)*(32*z+34); put(img,load(f),x,y,z); d.text((x+4,y+32*z+4),label,font=F,fill=AMBER if 'x' else TXT)
    img.save(os.path.join(out,name+'.png'))
spec_names=['Duck','Rabbit','Human','Turtle','Fox','Owl','Mouse','Cat','Frog','Elf','Bear','Wolf','Dog','Panda','Boar','Pig','Goat','Deer','Raccoon','Monkey','Orc','Goblin','Lizard','Skeleton','Dwarf']
grid('especies',[('species-'+n,n.lower()) for n in spec_names],9,'25 espécies. Uma linguagem visual.','Mesma classe, mesma luz, mesmo contorno: só a espécie muda.',5)
cls=sorted(glob.glob(src+'/class-*.png'))
grid('classes',[(os.path.basename(f)[:-4],os.path.basename(f)[9:-4]) for f in cls],10,'Uma silhueta para cada função.','20 classes visuais · arma, roupa e adorno mudam; o corpo da espécie permanece.',5)
items=[]
for c in ['knight','mage','archer','cleric']:
    for g,gn in enumerate(['recruta','veterano','elite','herói']): items.append((f'rank-{c}-{g}',f'{c} · {gn}'))
grid('patentes',items,8,'Recruta, Veterano, Elite, Herói.','Mesmo personagem, equipamento cada vez mais elaborado (só visual).',5)
seeds=[(f'seed-{i:02d}',f'semente {i}') for i in range(1,46)]
grid('sementes',seeds,9,'45 sementes, 45 personagens.','ForgeCast.Generate(seed): espécie × classe × patente sorteadas em fluxos separados.',4)
# mude uma parte
items=[('slot-base','base')]
for s in ['hat','weapon','offhand','back','outfit']:
    for k in (1,2): items.append((f'slot-{s}-{k}',f'{s} #{k}'))
grid('slots',items,6,'Mude uma parte. Mantenha o personagem.','Cada slot tem seu próprio fluxo de semente (base 2042); a espécie e o resto ficam como estão.',5)
# equipes
grid('equipes',[(f'team-{t}',f'equipe {t+1}') for t in range(6)],6,'Mesma forma, seis cores de equipe.','Só o material de equipe muda.',6)
# movimento
def strips(name,ids,motions,title,sub,z=3):
    rows=[]
    for i in ids:
        for m in motions:
            p=os.path.join(src,f'strip-{i}-{m}.png')
            if os.path.exists(p): rows.append((i,m,Image.open(p).convert('RGBA')))
    W=max(r[2].width for r in rows)*z+200; H=130+len(rows)*(32*z+10)+30
    img=Image.new('RGB',(W,H),BG); d=ImageDraw.Draw(img); head(d,'VERDANT / ESTILO V2',title,sub,W)
    for k,(i,m,im) in enumerate(rows):
        y=118+k*(32*z+10); d.text((20,y+32*z//2-8),f'{i}\n{m}',font=F,fill=AMBER); put(img,im,190,y,z)
    img.save(os.path.join(out,name+'.png'))
strips('movimento',['bram','suri','wolf-barb','orc-hal'],['Idle','Walk','Attack','Hurt','Death'],'O movimento também é gerado.','Idle · caminhada · golpe em 12 quadros com peso por pose · hit em xadrez · morte em queda')
# direções
items=[]
for i in ['bram','suri','wolf-barb','orc-hal']:
    for f in ['South','East','North','West']: items.append((f'dir-{i}-{f}',f'{i} {f.lower()}'))
grid('direcoes',items,8,'Quatro direções.','Sul · leste · norte (costas) · oeste (espelho do leste).',5)
print('ok')
