(()=>{'use strict';
const host=document.getElementById('stage10Analytics');if(!host)return;
const lang=()=>document.documentElement.lang==='fa'?'fa':'en';
const t=(en,fa)=>lang()==='fa'?fa:en;
const num=(n)=>new Intl.NumberFormat(lang()==='fa'?'fa-IR':'en-US').format(n);
const el=(tag,cls,txt)=>{const n=document.createElement(tag);if(cls)n.className=cls;if(txt!==undefined)n.textContent=txt;return n};
const btn=el('button','', 'Refresh analytics');btn.type='button';
const notice=el('p','muted','Sign in to view the last 30 days.');notice.setAttribute('role','status');
const cards=el('div','metrics');const chart=el('div','analytics-bars');const sales=el('div','analytics-sales');
host.append(btn,notice,cards,sales,chart);
async function load(){btn.disabled=true;notice.textContent=t('Loading…','در حال بارگذاری…');
 try{const res=await fetch('/api/admin/analytics',{credentials:'same-origin',headers:{Accept:'application/json'}});
 if(!res.ok)throw Error(res.status===401?t('Admin sign-in required','ورود مدیر الزامی است'):t('Analytics unavailable','گزارش در دسترس نیست'));
 const data=await res.json();cards.replaceChildren();chart.replaceChildren();sales.replaceChildren();
 for(const [en,fa,value] of [['Orders','سفارش‌ها',data.orders],['Pending','در انتظار',data.pending],['Preparing','در حال آماده‌سازی',data.preparing],['Ready','آماده',data.ready]]){
 const c=el('div','metric');c.append(el('span','',t(en,fa)),el('strong','',num(value)));cards.append(c)}
 sales.append(el('h3','',t('Completed sales (separate currencies)','فروش تکمیل‌شده (ارزهای مستقل)')));
 for(const x of data.sales){sales.append(el('p','',`${x.currency}: ${num(x.revenue)} · ${num(x.count)} ${t('orders','سفارش')}`))}
 if(!data.sales.length)sales.append(el('p','muted',t('No completed orders in the last 30 days','در ۳۰ روز گذشته سفارش تکمیل‌شده‌ای وجود ندارد')));
 chart.append(el('h3','',t('Daily order volume','تعداد سفارش‌های روزانه')));
 const max=Math.max(1,...data.daily.map(x=>x.orders));
 for(const d of data.daily){const line=el('div','analytics-day');const label=el('span','',d.date);const track=el('div','analytics-track');const fill=el('div','analytics-fill');fill.style.width=`${100*d.orders/max}%`;track.append(fill);line.append(label,track,el('span','',num(d.orders)));chart.append(line)}
 notice.textContent=t('Last 30 days · completed orders only count toward sales','۳۰ روز گذشته · فقط سفارش‌های تکمیل‌شده جزو فروش محسوب می‌شوند');
 }catch(err){notice.textContent=err.message}finally{btn.disabled=false}}
 btn.addEventListener('click',load);
 const observer=new MutationObserver(()=>{btn.textContent=t('Refresh analytics','به‌روزرسانی گزارش')});observer.observe(document.documentElement,{attributes:true,attributeFilter:['lang']});
 btn.textContent=t('Refresh analytics','به‌روزرسانی گزارش');
})();
