/* global process, Buffer, URL, console, E4Probe, window, AwakeningLab */
// Local integration only: exported test harness + disposable Django receiver.
// node godot/tests/playtest-web.mjs <web-dir> <key.pem> <cert.pem> <evidence.json>
import assert from 'node:assert/strict';
import {createServer as httpsServer} from 'node:https';
import {request as httpRequest} from 'node:http';
import {readFile,writeFile} from 'node:fs/promises';
import {resolve,extname} from 'node:path';
import {chromium} from '@playwright/test';
const [webDir,keyFile,certFile,evidenceFile]=process.argv.slice(2);
const tls={key:await readFile(keyFile),cert:await readFile(certFile)};
const attempts=[];let dropNext=true;
const api=httpsServer(tls,(req,res)=>{
  const chunks=[];req.on('data',chunk=>chunks.push(chunk));req.on('end',async()=>{
    const body=Buffer.concat(chunks);
    let durable=true;
    if(req.method==='POST') {
      durable=await page.evaluate(async wire=>{
        const value=await new Promise((resolve,reject)=>{
          const tx=AwakeningLab.db.transaction('snapshots','readonly');
          const request=tx.objectStore('snapshots').get('user://e4-browser-probe.json');
          request.onsuccess=()=>resolve(request.result);request.onerror=()=>reject(request.error);
        });
        return JSON.parse(value).playtest.jobs.some(job=>job.body===wire);
      },body.toString()).catch(()=>false);
    }
    const proxy=httpRequest({host:'127.0.0.1',port:58483,path:req.url,method:req.method,
      headers:{...req.headers,host:'127.0.0.1:58483'}},upstream=>{
      const response=[];upstream.on('data',chunk=>response.push(chunk));upstream.on('end',()=>{
        if(req.method==='POST') attempts.push({body:body.toString(),status:upstream.statusCode,durable_before_post:durable});
        if(req.method==='POST'&&dropNext){dropNext=false;res.destroy();return;}
        res.writeHead(upstream.statusCode,upstream.headers);res.end(Buffer.concat(response));
      });
    });proxy.on('error',()=>{res.writeHead(503);res.end();});proxy.end(body);
  });
});
const mime={'.html':'text/html','.js':'text/javascript','.wasm':'application/wasm','.pck':'application/octet-stream','.png':'image/png'};
const site=httpsServer(tls,async(req,res)=>{
  const path=resolve(webDir,'.'+(req.url==='/'?'/index.html':new URL(req.url,'https://localhost').pathname));
  if(!path.startsWith(resolve(webDir)+'/')){res.writeHead(403);res.end();return;}
  try{const body=await readFile(path);res.writeHead(200,{'Content-Type':mime[extname(path)]??'application/octet-stream','Cache-Control':'no-store'});res.end(body);}
  catch{res.writeHead(404);res.end();}
});
await Promise.all([new Promise(r=>api.listen(58484,'127.0.0.1',r)),new Promise(r=>site.listen(58485,'127.0.0.1',r))]);
const browser=await chromium.launch({headless:process.env.GTC_E4_HEADED!=="1",executablePath:process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE,args:['--ignore-certificate-errors']});
const context=await browser.newContext({ignoreHTTPSErrors:true,viewport:{width:1600,height:900}});
const errors=[];
const open=async(parent)=>{const page=parent ? (await Promise.all([context.waitForEvent('page'),parent.evaluate(()=>window.open('https://127.0.0.1:58485/','_blank'))]))[0] : await context.newPage();page.on('console',msg=>{if(msg.type()==='error'){errors.push(msg.text());console.error(msg.text());}});page.on('pageerror',err=>(errors.push(String(err)),console.error(String(err))));if(!parent)await page.goto('https://127.0.0.1:58485/');await page.waitForFunction(()=>globalThis.E4Probe?.ready&&globalThis.E4Probe.result,undefined,{timeout:45000});return page;};
const state=page=>page.evaluate(()=>JSON.parse(E4Probe.result));
const command=async(page,...ops)=>{await page.evaluate(ops=>ops.forEach(op=>E4Probe.command(typeof op==='string'?{op}:op)),ops);await page.waitForTimeout(120);return state(page);};
let page;
try {
 page=await open();let initial=await state(page);assert(initial.writable&&!initial.blocked);assert.equal(initial.confirmed,false);
 await page.screenshot({path:evidenceFile+'.consent.png'});
 await command(page,'legacy');await page.waitForTimeout(500);await page.close();
 page=await open();const legacy=await state(page);
 assert.equal(legacy.save.state?.seed,'e4-legacy','legacy Godot filesystem save survives browser reload and migration');
 assert.equal(legacy.save.state.status,'playing');assert.equal(legacy.pending,0);
 await command(page,'begin','place','act');await page.waitForTimeout(300);
 assert.equal(attempts.length,0,'playing without consent sends nothing');assert.equal((await state(page)).pending,0);
 await command(page,'join');let partial=await state(page);assert.equal(partial.save.playtest.runs[partial.save.playtest.active].context.coverage,'partial');
 // New observation starts before this new run and records genuine initial choices.
 await command(page,'begin','place','act','pause');
 await page.waitForFunction(()=>JSON.parse(E4Probe.result).save.playtest.jobs.some(j=>j.attempts>0));
 await page.waitForTimeout(300);let planning=(await state(page)).clock.planning_ms;assert(planning>=200,'paused gameplay counts planning');
 await command(page,'play');await page.waitForTimeout(300);assert((await state(page)).clock.simulation_ms>=200,'running simulation counts separately');
 const beforeMenu=await state(page);await command(page,'menu');await page.waitForTimeout(300);const menu=await state(page);assert(menu.clock.planning_ms-beforeMenu.clock.planning_ms>=200,'open game menu counts planning');assert.equal(menu.clock.excluded_ms,beforeMenu.clock.excluded_ms,'menu does not add excluded time');
 // Playwright forces every page focused/visible by default; disable that override.
 const focusSession=await context.newCDPSession(page);
 await focusSession.send('Emulation.setFocusEmulationEnabled',{enabled:false});
 const second=await open(page);
 await (await context.newCDPSession(second)).send('Emulation.setFocusEmulationEnabled',{enabled:false});assert.equal((await state(second)).writable,false,'second tab cannot save or send');await command(second,'join','begin','place','act');assert.equal((await state(second)).confirmed,false);assert.equal((await state(second)).pending,0);
 await second.bringToFront();await second.waitForTimeout(400);

 await page.bringToFront();await page.waitForTimeout(200);
 
 const background=(await state(page)).clock.background_ms;assert(background>=300,'browser background does not count as gameplay');await second.close();
 // Outage and reopening: retain an attempted immutable body, then retry after consent.
 await context.route('https://127.0.0.1:58484/**',route=>route.abort());
 await command(page,'act','flush');await page.waitForTimeout(500);
 await page.waitForFunction(()=>{const s=JSON.parse(E4Probe.result);return s.saved_generation===s.generation;});
 const before=(await state(page)).save.playtest;assert(before.jobs.length>0);const pending=before.jobs.map(j=>({id:j.id,body:j.body}));
 await page.screenshot({path:evidenceFile+'.game.png'});await page.close();
 page=await open();let restored=await state(page);assert(!restored.confirmed,'reopening awaits consent');
 for(const job of pending)assert.equal(restored.save.playtest.jobs.find(j=>j.id===job.id)?.body,job.body,'reload preserves pending bytes');
 await context.unroute('https://127.0.0.1:58484/**');await command(page,'join','resume','pause','flush');
 await page.waitForFunction(()=>JSON.parse(E4Probe.result).pending===0,undefined,{timeout:45000});
 let recovered=await state(page);assert(recovered.status.includes('Receipt confirmed'),'receipt becomes confirmed only after local commit');
 assert(attempts.some(a=>a.status===200),'a committed response lost in transit retries idempotently');
 assert(attempts.every(entry=>entry.durable_before_post),'all requests were already committed in IndexedDB');
 const duplicated=attempts.filter((entry,index)=>attempts.findIndex(a=>a.body===entry.body)<index);assert(duplicated.length>0);
 for(const entry of duplicated)assert.equal(entry.status,200);
 const run=restored.save.playtest.active;
 const live=recovered.save.playtest.runs[run];assert(live.sessions>=2,'resume opens a new session');
 await command(page,'withdraw');await page.waitForTimeout(300);const withdrawn=await state(page);assert.equal(withdrawn.pending,0);assert.equal(withdrawn.confirmed,false);
 const noStorage=await browser.newContext({ignoreHTTPSErrors:true,viewport:{width:1600,height:900}});
 await noStorage.addInitScript(()=>Object.defineProperty(globalThis,'indexedDB',{value:undefined}));
 const memoryPage=await noStorage.newPage();await memoryPage.goto('https://127.0.0.1:58485/');
 await memoryPage.waitForFunction(()=>globalThis.E4Probe?.result);
 await command(memoryPage,'join','begin','place','act');const memory=await state(memoryPage);
 assert(memory.blocked&&!memory.confirmed&&memory.pending===0,'storage failure allows unobserved in-memory gameplay');
 assert.equal(memory.save.state.status,'playing');await noStorage.close();
 await writeFile(evidenceFile,JSON.stringify({engine:'Godot 4.7.2 Web release, Chromium',origin:'https://127.0.0.1:58485',endpoint:'https://127.0.0.1:58484',no_consent_no_requests:true,partial_legacy:true,legacy_filesystem_save_loaded:true,planning_ms:planning,menu_counts_planning:true,background_ms:background,second_tab_readonly:true,durable_before_post:attempts.every(entry=>entry.durable_before_post),reload_preserves_bytes:true,retry_same_body:true,statuses:attempts.map(a=>a.status),renewed_consent:true,resume_sessions:live.sessions,confirmed_receipt:recovered.save.playtest.receipt,withdraw_clears_pending:true,storage_failure_allows_gameplay:true,errors,real_phone:false,itch_io:false},null,2));
 assert.deepEqual(errors.filter(e=>!e.includes('Failed to load resource')&&!e.includes('ERR_FAILED')&&!e.includes('ERR_EMPTY_RESPONSE')),[],'no game runtime errors');
 console.log('PASS web consent, clocks, tabs, persisted retries, CORS/HTTP receipt and withdrawal');
} catch(error) {console.error('Diagnostics:',errors);if(page&&!page.isClosed())await page.screenshot({path:evidenceFile+'.failure.png'});throw error;} finally {await context.close();await browser.close();api.closeAllConnections();site.closeAllConnections();await Promise.all([new Promise(r=>api.close(r)),new Promise(r=>site.close(r))]);}
