(()=>{'use strict';const panel=document.getElementById('inventoryPanel');if(!panel)return;
const rows=document.getElementById('inventoryRows'),notice=document.getElementById('inventoryNotice'),refresh=document.getElementById('refreshInventory');
const fa=()=>document.documentElement.lang==='fa',t=(en,faText)=>fa()?faText:en;
const node=(tag,text)=>{const e=document.createElement(tag);if(text!==undefined)e.textContent=text;return e};
async function load(){refresh.disabled=true;notice.textContent=t('Loading inventory…','در حال دریافت موجودی…');
try{const r=await fetch('/api/admin/inventory',{credentials:'same-origin'});if(!r.ok)throw Error(t('Sign in as admin to view inventory','برای مشاهده موجودی با حساب مدیر وارد شوید'));
const data=await r.json();rows.replaceChildren();for(const item of data){const tr=node('tr');tr.append(node('td',String(item.id)),node('td',fa()?item.nameFa:item.nameEn));
const td=node('td'),input=node('input');input.type='number';input.min='0';input.max='1000000';input.step='1';input.placeholder=t('Unlimited','نامحدود');input.value=item.stockQuantity??'';input.setAttribute('aria-label',t('Stock quantity','تعداد موجودی'));td.append(input);if(item.lowStock)td.append(node('span',t(' ⚠ Low',' ⚠ کم')));tr.append(td);
const action=node('td'),button=node('button',t('Save','ذخیره'));button.type='button';button.addEventListener('click',async()=>{const value=input.value.trim();if(value!==''&&(!/^\d+$/.test(value)||Number(value)>1000000)){notice.textContent=t('Invalid quantity','تعداد نامعتبر');return}button.disabled=true;try{const r=await fetch('/api/admin/inventory/'+item.id,{method:'PATCH',credentials:'same-origin',headers:{'Content-Type':'application/json','X-Restaurant-Admin':'1'},body:JSON.stringify({stockQuantity:value===''?null:Number(value)})});if(!r.ok)throw Error(t('Update failed','به‌روزرسانی ناموفق بود'));await load()}catch(e){notice.textContent=e.message}finally{button.disabled=false}});action.append(button);tr.append(action);rows.append(tr)}notice.textContent=t('Inventory loaded','موجودی بارگذاری شد')}
catch(e){notice.textContent=e.message}finally{refresh.disabled=false}}
refresh.addEventListener('click',load);
})();