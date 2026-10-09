/* Stage 3: illustrative menu pricing, not a live FX feed. */
(()=>{'use strict';
const RATE=100000; // Example only: 100,000 toman per USD. Replace with approved menu prices before launch.
const fa=document.documentElement.lang.toLowerCase().startsWith('fa');
const normalize=value=>Number(String(value??'').replace(/[۰-۹٠-٩]/g,c=>{const p='۰۱۲۳۴۵۶۷۸۹'.indexOf(c);return p>=0?p:'٠١٢٣٤٥٦٧٨٩'.indexOf(c)}).replace(/[^0-9.]/g,''))||0;
const money=value=>fa?new Intl.NumberFormat('fa-IR',{maximumFractionDigits:0}).format(normalize(value))+' تومان':new Intl.NumberFormat('en-US',{style:'currency',currency:'USD',maximumFractionDigits:2}).format(normalize(value)/RATE);
window.AshkanPricing=Object.freeze({format:money,normalize,rate:RATE});
document.querySelectorAll('.dish').forEach(card=>{const price=card.querySelector('[data-price]');const amount=price?.dataset.price;const label=card.querySelector('.dish-top strong');if(label&&amount)label.textContent=money(amount)});
document.querySelectorAll('.food-card').forEach(card=>{const amount=card.querySelector('[data-price]')?.dataset.price;const label=card.querySelector('.food-price');if(label&&amount)label.textContent=money(amount)});
})();