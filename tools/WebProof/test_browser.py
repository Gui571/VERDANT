import json, argparse
parser=argparse.ArgumentParser()
parser.add_argument("--url",default="http://127.0.0.1:8765")
parser.add_argument("--fixture",required=True)
parser.add_argument("--chromium",default="/usr/bin/chromium")
parser.add_argument("--capture",default="web-proof.png")
args=parser.parse_args()
from pathlib import Path
from playwright.sync_api import sync_playwright
with sync_playwright() as p:
 browser=p.chromium.launch(executable_path=args.chromium,headless=True,args=['--no-sandbox'])
 page=browser.new_page(viewport={'width':1440,'height':960},has_touch=True)
 page.on('pageerror',lambda e:print('PAGE_ERROR',e))
 page.goto(args.url)
 page.wait_for_function('window.proof?.ready || window.proofError',timeout=90000)
 print(page.locator('#status').inner_text())
 assert page.evaluate('window.proofError') is None
 page.wait_for_function('window.proof.state().tick>=65',timeout=30000)
 page.screenshot(path=args.capture)
 page.evaluate('window.proof.reset()')
 trace=json.loads(page.evaluate('window.proof.finish()'))
 expected=json.loads(Path(args.fixture).read_text())
 print('WEB_TRACE',len(trace),'events','equal',trace==expected)
 assert trace==expected
 for width,height in [(1280,720),(1440,720),(1600,720)]:
  page.set_viewport_size({'width':width,'height':height})
  for selector in ['#play','#reset','#special']:
   box=page.locator(selector).bounding_box();assert box['width']>=48 and box['height']>=48
  page.locator('#reset').tap()
  page.wait_for_function('window.proof.state().tick===0')
  print('WEB_TOUCH_PASS',width,height)
 browser.close()
