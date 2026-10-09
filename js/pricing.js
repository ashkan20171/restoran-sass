/* Stage 4: independent menu price lists; browser-only demo settings. */
(()=>{'use strict';
const DEFAULT_RATE=100000;
const normalize=v=>Number(String(v??'').replace(/[۰-۹٠-٩]/g,c=>{let i='۰۱۲۳۴۵۶۷۸۹'.indexOf(c);return i>=0?i:'٠١٢٣٤٥٦٧٨٩'.indexOf(c)}).replace(/[^0-9.]/g,''))||0;
const fa=document.documentElement.lang.toLowerCase().startsWith('fa');
const money=(value,currency=fa?'IRT':'USD')=>currency==='IRT'?new Intl.NumberFormat('fa-IR').format(normalize(value))+' تومان':new Intl.NumberFormat('en-US',{style:'currency',currency:'USD'}).format(normalize(value));
let overrides={};try{overrides=JSON.parse(localStorage.getItem('ashkan-price-overrides-v4')||'{}')}catch{}
const price=(toman,usd,id)=>{const custom=id&&overrides[id];return fa?normalize(custom?.toman??toman):normalize(custom?.usd??usd??(normalize(toman)/DEFAULT_RATE))};
const display=(toman,usd,id)=>money(price(toman,usd,id));
window.AshkanPricing=Object.freeze({format:v=>money(fa?v:v/DEFAULT_RATE),normalize,rate:DEFAULT_RATE,display,price,money,fa});
document.querySelectorAll('.dish,.food-card').forEach((card,i)=>{const source=card.querySelector('[data-price]');if(!source)return;const toman=normalize(source.dataset.price);const usd=normalize(source.dataset.usd||toman/DEFAULT_RATE);const id=source.dataset.itemId||card.dataset.name||'item-'+i;const label=card.querySelector('.dish-top strong,.food-price');if(label)label.textContent=display(toman,usd,id);source.dataset.usd=String(usd);source.dataset.itemId=id;const add=card.querySelector('[data-add]');if(add){add.dataset.price=String(toman);add.dataset.usd=String(usd);add.dataset.itemId=id}});
})();
