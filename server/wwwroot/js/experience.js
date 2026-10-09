(()=>{'use strict';
const fa=document.documentElement.lang==='fa';
const translations=fa?{top:'بازگشت به بالا',privacy:'این وب‌سایت نمایشی است؛ رزرو و سفارش واقعی ثبت نمی‌شود.'}:{top:'Back to top',privacy:'This is a demonstration website; no real reservations or orders are submitted.'};
const back=document.createElement('button');back.className='back-to-top';back.type='button';back.textContent='↑';back.setAttribute('aria-label',translations.top);back.title=translations.top;document.body.append(back);
const sync=()=>{back.hidden=window.scrollY<450};window.addEventListener('scroll',sync,{passive:true});sync();back.addEventListener('click',()=>window.scrollTo({top:0,behavior:matchMedia('(prefers-reduced-motion: reduce)').matches?'instant':'smooth'}));
// Keep language routes paired without tracking users or overwriting their chosen language.
const switcher=document.querySelector('.lang-switch');if(switcher){switcher.setAttribute('aria-label',fa?'Switch website to English':'تغییر زبان سایت به فارسی');}
// Announce demo status clearly to assistive technology.
const footer=document.querySelector('footer');if(footer){const note=document.createElement('p');note.className='demo-disclosure';note.textContent=translations.privacy;footer.append(note)}
})();
