import axios from 'axios';

const service = axios.create({
    timeout: 10000,
    baseURL: 'http://172.19.229.149:5000/api',  // 完整后端地址
});

// 请求拦截器 - 添加 token
service.interceptors.request.use(
    config => {
        const token = localStorage.getItem('adminToken');
        // 发送Authorization头
        if (token) {
            config.headers['Authorization'] = `Bearer ${token}`;
        }
        return config;
    },
    error => {
        console.log(error);
        return Promise.reject();
    }
);

// 响应拦截器 - 统一处理响应格式
service.interceptors.response.use(
    response => {
        if (response.status === 200) {
            const res = response.data;
            // 兼容多种后端响应格式
            // 格式1: { success: true, message: "", data: {} }
            // 格式2: { code: 200, msg: "", data: {} }
            // 格式3: { statusCode: 1, message: "", data: {} }
            let status_code, msg, data;

            if (res.success !== undefined) {
                // 格式1
                status_code = res.success ? 1 : 0;
                msg = res.message || '';
                data = res.data;
            } else if (res.code !== undefined) {
                // 格式2
                status_code = res.code === 200 || res.code === 0 ? 1 : 0;
                msg = res.msg || res.message || '';
                data = res.data;
            } else if (res.statusCode !== undefined) {
                // 格式3
                status_code = res.statusCode === 1 ? 1 : 0;
                msg = res.message || '';
                data = res.data;
            } else {
                // 未知格式，直接返回
                status_code = 1;
                msg = '';
                data = res;
            }

            return {
                status_code: status_code,
                msg: msg,
                data: data
            };
        } else {
            return Promise.reject();
        }
    },
    error => {
        console.log('请求错误:', error);
        if (error.response) {
            const status = error.response.status;
            // 打印后端返回的具体错误信息
            console.log('错误状态码:', status);
            console.log('错误响应数据:', error.response.data);

            if (status === 400) {
                const data = error.response.data;
                const msg = data.message || data.msg || data.errors || JSON.stringify(data);
                console.log('400错误详情:', msg);
            }

            if (status === 401) {
                // token过期或无效，跳转登录
                localStorage.removeItem('adminToken');
                localStorage.removeItem('adminInfo');
                window.location.href = '/#/login-admin';
            }
        }
        return Promise.reject(error);
    }
);

export default service;