
(() => {
  "use strict";
  const $ = (s, r=document) => r.querySelector(s);
  const $$ = (s, r=document) => [...r.querySelectorAll(s)];
  const toast = (message) => {
    const el = $("#toast"); if (!el) return;
    el.textContent = message; el.classList.add("show");
    clearTimeout(window.__toast); window.__toast=setTimeout(()=>el.classList.remove("show"),2600);
  };

  // Mobile navigation
  const menuToggle=$("#menuToggle"), navLinks=$("#navLinks");
  menuToggle?.addEventListener("click",()=>{
    const open=navLinks.classList.toggle("mobile-open");
    menuToggle.setAttribute("aria-expanded", String(open));
  });
  $$(".nav-links a").forEach(a=>a.addEventListener("click",()=>navLinks.classList.remove("mobile-open")));

  // Smooth same-page links
  $$('a[href^="#"]').forEach(a=>a.addEventListener("click",e=>{
    const id=a.getAttribute("href"); if(id && id!=="#"){const target=$(id); if(target){e.preventDefault();target.scrollIntoView({behavior:"smooth",block:"start"});}}
  }));

  // Reveal on scroll
  const observer = new IntersectionObserver(entries=>entries.forEach(e=>e.isIntersecting&&e.target.classList.add("visible")),{threshold:.08});
  $$(".reveal").forEach(el=>observer.observe(el));

  // Menu filtering/search
  const filters=$$(".filter"), cards=$$(".food-card[data-category]");
  const search=$("#menuSearch");
  function filterMenu(){
    const active=$(".filter.active")?.dataset.filter || "all", q=(search?.value||"").trim().toLowerCase();
    cards.forEach(card=>{
      const okCat=active==="all"||card.dataset.category===active;
      const okText=(card.dataset.name||"").toLowerCase().includes(q);
      card.hidden=!(okCat&&okText);
    });
  }
  filters.forEach(btn=>btn.addEventListener("click",()=>{filters.forEach(x=>x.classList.remove("active"));btn.classList.add("active");filterMenu()}));
  search?.addEventListener("input",filterMenu);

  // Reservation modal
  const modal=$("#reservationModal");
  function openModal(){modal?.classList.add("open");document.body.classList.add("lock");}
  function closeModal(){modal?.classList.remove("open");document.body.classList.remove("lock");}
  $$("[data-open-reservation]").forEach(b=>b.addEventListener("click",openModal));
  $$(".modal [data-close]").forEach(b=>b.addEventListener("click",closeModal));
  modal?.addEventListener("click",e=>{if(e.target===modal)closeModal()});
  document.addEventListener("keydown",e=>{if(e.key==="Escape")closeModal()});
  $("#reservationForm")?.addEventListener("submit",e=>{
    e.preventDefault(); const fd=new FormData(e.target);
    const name=fd.get("name"), date=fd.get("date"), time=fd.get("time");
    toast(`درخواست رزرو برای ${name} در تاریخ ${date} ساعت ${time} ثبت شد.`);
    e.target.reset(); closeModal();
  });

  // Demo cart
  let cart=JSON.parse(localStorage.getItem("restaurant-cart")||"[]");
  const cartCount=()=>{const el=$("#cartCount");if(el)el.textContent=cart.length};
  $$("[data-add]").forEach(btn=>btn.addEventListener("click",()=>{
    cart.push({name:btn.dataset.add,price:window.AshkanPricing.normalize(btn.dataset.price)});
    localStorage.setItem("restaurant-cart",JSON.stringify(cart));cartCount();
    toast(`«${btn.dataset.add}» به سفارش شما اضافه شد.`);
  }));
  cartCount();

  // Chatbot - client-side intent assistant. Replace reply() with API call for a real LLM.
  const chat=$("#chatWindow"), launch=$("#chatLaunch"), close=$("#chatClose"), form=$("#chatForm"), input=$("#chatInput"), messages=$("#chatMessages");
  const addMessage=(text,who="bot")=>{const el=document.createElement("div");el.className=`bubble ${who}`;el.textContent=text;messages?.appendChild(el);messages?.scrollTo({top:messages.scrollHeight,behavior:"smooth"})};
  const reply=(text)=>{
    const t=text.toLowerCase();
    if(t.includes("رزرو")||t.includes("میز")) return "حتماً 🌟 از دکمه «رزرو میز» استفاده کنید؛ تعداد نفرات، تاریخ و ساعت را وارد کنید تا درخواستتان ثبت شود.";
    if(t.includes("منو")||t.includes("غذا")) return "منوی ما شامل پیش‌غذا، غذای اصلی، پاستا، پیتزا و دسر است. اگر بگویید چه طعمی دوست دارید، چند پیشنهاد می‌دهم.";
    if(t.includes("قیمت")) return "قیمت‌ها کنار هر غذا نمایش داده شده‌اند و در صفحه منو می‌توانید بر اساس دسته‌بندی جست‌وجو کنید.";
    if(t.includes("آدرس")||t.includes("کج")) return "آدرس نمونه رستوران: تهران، خیابان ولیعصر، کوچه هنر، پلاک ۲۴. برای اطلاعات تماس صفحه تماس با ما را ببینید.";
    if(t.includes("ساعت")||t.includes("باز")) return "ساعات کاری: هر روز ۱۲ تا ۲۳:۳۰. جمعه‌ها تا ۲۴ پذیرای شما هستیم.";
    if(t.includes("تخفیف")||t.includes("پیشنهاد")) return "پیشنهاد ویژه امروز: ۱۵٪ تخفیف روی منوی دو نفره. کد نمونه: GOLD15";
    if(t.includes("سلام")||t.includes("hello")||t.includes("hi")) return "سلام! 👋 من دستیار رستوران هستم. درباره منو، رزرو، ساعات کاری یا پیشنهاد غذا از من بپرسید.";
    return "متوجه شدم 😊 می‌توانم درباره منو، قیمت‌ها، رزرو میز، ساعات کاری، آدرس و پیشنهادهای ویژه راهنمایی‌تان کنم.";
  };
  launch?.addEventListener("click",()=>{chat.classList.toggle("open");if(chat.classList.contains("open"))input?.focus()});
  close?.addEventListener("click",()=>chat.classList.remove("open"));
  form?.addEventListener("submit",e=>{e.preventDefault();const v=input.value.trim();if(!v)return;addMessage(v,"user");input.value="";setTimeout(()=>addMessage(reply(v)),350)});
  $$(".quick").forEach(b=>b.addEventListener("click",()=>{addMessage(b.textContent,"user");setTimeout(()=>addMessage(reply(b.textContent)),300)}));

  // Contact / generic forms
  $$("form[data-demo-form]").forEach(f=>f.addEventListener("submit",e=>{e.preventDefault();toast("پیام شما با موفقیت ثبت شد. از همراهی شما سپاسگزاریم.");f.reset()}));

  // Active page link
  const current=(location.pathname.split("/").pop()||"index.html");
  $$(".nav-links a").forEach(a=>{if(a.getAttribute("href")===current)a.classList.add("active")});

  // Year
  $$(".year").forEach(el=>el.textContent=new Date().getFullYear());
})();
