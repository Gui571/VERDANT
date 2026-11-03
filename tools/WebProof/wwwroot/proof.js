const field=document.querySelector('#field'), ctx=field.getContext('2d'), status=document.querySelector('#status');
let state, sprites=[], running=true, busy=false, accumulator=0, previous=0;
const call=(method,...args)=>DotNet.invokeMethodAsync('WebProof',method,...args);
function resize(){const scale=Math.max(1,Math.floor(Math.min((innerWidth-24)/480,(innerHeight-230)/270)));field.style.width=`${480*scale}px`;field.style.height=`${270*scale}px`;}
addEventListener('resize',resize);resize();
function paint(){
 ctx.fillStyle='#82985a';ctx.fillRect(0,0,480,270);ctx.fillStyle='#92784c';ctx.fillRect(24,28,432,218);ctx.imageSmoothingEnabled=false;
 for(const u of state.units){if(u.hp<=0)continue;const sprite=sprites[u.parent>=0?u.parent:u.id];if(!sprite)continue;
  const x=Math.round(u.x+80),y=Math.round(u.y+70);ctx.drawImage(sprite.frames[Math.floor(state.tick*8/30)%8],x-sprite.size/2,y-sprite.size+3);
  ctx.fillStyle='#151829';ctx.fillRect(x-12,y-sprite.size-3,24,3);ctx.fillStyle=u.team===0?'#8ec2f6':'#e991aa';ctx.fillRect(x-11,y-sprite.size-2,22*u.hp/u.max,1);
 }
 status.textContent=`Tick ${state.tick} · ${state.events} eventos · ${state.finished?'Fim: time '+(state.winner+1):'30 passos/s · semente 42'}`;
 const bram=state.units[0];document.querySelector('#special').disabled=state.finished||bram.hp<=0||bram.cooldown>0;
}
async function reset(){const data=await call('Reset');sprites=data.sprites.map(s=>({size:s.size,frames:s.frames.map(base64=>{
 const bytes=typeof base64==='string'?Uint8ClampedArray.from(atob(base64),c=>c.charCodeAt(0)):new Uint8ClampedArray(base64);const c=document.createElement('canvas');c.width=c.height=s.size;c.getContext('2d').putImageData(new ImageData(bytes,s.size,s.size),0,0);return c;
})}));state=data.state;accumulator=0;paint();}
window.proofReady=async()=>{
 try{await reset();window.proof={ready:true,async finish(){running=false;while(busy)await new Promise(r=>setTimeout(r,10));state=await call('Advance',1800);paint();return await call('Trace');},async reset(){running=false;while(busy)await new Promise(r=>setTimeout(r,10));await reset();},state:()=>state};requestAnimationFrame(frame);}catch(e){status.textContent=String(e);window.proofError=String(e);}
};
async function frame(time){const dt=previous?Math.min((time-previous)/1000,.25):0;previous=time;
 if(running&&!state.finished&&!busy){accumulator+=dt;const ticks=Math.floor(accumulator*30);if(ticks){accumulator-=ticks/30;busy=true;try{state=await call('Advance',ticks);paint();}finally{busy=false;}}}requestAnimationFrame(frame);}
document.querySelector('#play').onclick=()=>{running=!running;document.querySelector('#play').textContent=running?'Pausar':'Continuar';};
document.querySelector('#reset').onclick=async()=>{if(busy)return;busy=true;try{await reset();}finally{busy=false;}};
document.querySelector('#special').onclick=async()=>{if(busy)return;busy=true;try{await call('Special',0);state=await call('Advance',1);paint();}finally{busy=false;}};
