import Vue from 'vue';
import App from './App.vue';
import router from './router';
import ElementUI from 'element-ui';
import 'element-ui/lib/theme-chalk/index.css';

import api from './api/index.js';
Vue.prototype.$api = api;

// 管理员全局状态
let sta = {
    isLogin: false,
    adminName: ''
};

// 检查 localStorage 中是否有登录信息
const token = localStorage.getItem('adminToken');
const adminInfo = localStorage.getItem('adminInfo');
if (token && adminInfo) {
    sta.isLogin = true;
    try {
        const info = JSON.parse(adminInfo);
        sta.adminName = info.username || '管理员';
    } catch(e) {
        console.log(e);
    }
}

Vue.prototype.$sta = sta;
Vue.config.productionTip = false;

Vue.use(ElementUI, {
    size: 'medium'
});

// 路由守卫 - 管理员权限校验
router.beforeEach((to, from, next) => {
    document.title = `${to.meta.title}`;

    if (to.path === '/platform-admin') {
        if (!Vue.prototype.$sta.isLogin) {
            next('/login-admin');
        } else {
            next();
        }
    } else {
        next();
    }
});

new Vue({
    router,
    render: h => h(App)
}).$mount('#app');