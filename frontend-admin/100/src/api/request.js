import axios from 'axios';

const service = axios.create({
    timeout: 5000,
    baseURL: 'http://172.19.229.149:5000/api',  // 完整后端地址
    // withCredentials: true  // 注释掉这一行！
});

// 请求拦截器 - 添加 token
service.interceptors.request.use(
    config => {
        const token = localStorage.getItem('adminToken');
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
            return {
                status_code: res.success ? 1 : 0,
                msg: res.message,
                data: res.data
            };
        } else {
            return Promise.reject();
        }
    },
    error => {
        console.log('请求错误:', error);
        return Promise.reject(error);
    }
);

export default service;