const API_BASE = "http://172.19.229.149:5000/api";

let token = localStorage.getItem("token") || "";

export const setToken = (newToken) => {
  token = newToken;
  localStorage.setItem("token", token);
};

export const getToken = () => token;

export const clearToken = () => {
  token = "";
  localStorage.removeItem("token");
  localStorage.removeItem("user");
};

export const request = async (url, options = {}) => {
  const headers = {
    "Content-Type": "application/json",
    ...options.headers,
  };
  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  try {
    const response = await fetch(API_BASE + url, { ...options, headers });
    
    let data;
    const contentType = response.headers.get("content-type");
    if (contentType && contentType.includes("application/json")) {
      data = await response.json();
    } else {
      data = await response.text();
    }
    
    if (!response.ok) {
      const error = new Error(`HTTP ${response.status}`);
      error.response = { status: response.status, data };
      throw error;
    }
    
    console.log("✅ API 响应:", url, data);
    
    if (data && typeof data === 'object') {
      if (data.success === undefined) {
        return { success: true, data };
      }
      return data;
    }
    
    return { success: true, data };
  } catch (error) {
    console.error("❌ 请求失败:", url, error);
    
    let errorMessage = "网络异常：" + error.message;
    if (error.response?.data) {
      if (typeof error.response.data === 'object') {
        errorMessage = error.response.data.message || error.response.data.error || JSON.stringify(error.response.data);
      } else {
        errorMessage = error.response.data;
      }
    }
    
    return { 
      success: false, 
      message: errorMessage,
      status: error.response?.status 
    };
  }
};

export const authAPI = {
  register(data) {
    return request("/Auth/register", {
      method: "POST",
      body: JSON.stringify(data),
    });
  },
  login(data) {
    return request("/Auth/login", {
      method: "POST",
      body: JSON.stringify(data),
    });
  },
  adminLogin(data) {
    return request("/Auth/admin/login", {
      method: "POST",
      body: JSON.stringify(data),
    });
  },
};

export const goodsAPI = {
  getList(params = {}) {
    const query = [];
    if (params.keyword)
      query.push(`keyword=${encodeURIComponent(params.keyword)}`);
    if (params.categoryId) query.push(`categoryId=${params.categoryId}`);
    if (params.page) query.push(`page=${params.page}`);
    if (params.pageSize) query.push(`pageSize=${params.pageSize}`);
    let url = "/Goods" + (query.length ? "?" + query.join("&") : "");
    return request(url);
  },
  getDetail(id) {
    return request(`/Goods/${id}`);
  },
  publish(data) {
    return request("/Goods", {
      method: "POST",
      body: JSON.stringify(data),
    });
  },
  getMyGoods(params = {}) {
    const query = [];
    if (params.auditStatus !== undefined) query.push(`auditStatus=${params.auditStatus}`);
    if (params.page) query.push(`page=${params.page}`);
    if (params.pageSize) query.push(`pageSize=${params.pageSize}`);
    let url = "/Goods/my" + (query.length ? "?" + query.join("&") : "");
    return request(url);
  },
  update(id, data) {
    return request(`/Goods/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    });
  },
  remove(id) {
    return request(`/Goods/${id}`, {
      method: "DELETE",
    });
  },
};

export const tradeAPI = {
  create(data) {
    return request("/Trade", {
      method: "POST",
      body: JSON.stringify(data),
    });
  },
  getMyTrades(params = {}) {
    const query = [];
    if (params.status !== undefined) query.push(`status=${params.status}`);
    if (params.page) query.push(`page=${params.page}`);
    if (params.pageSize) query.push(`pageSize=${params.pageSize}`);
    let url = "/Trade" + (query.length ? "?" + query.join("&") : "");
    return request(url);
  },
  complete(id) {
    return request(`/Trade/${id}/complete`, {
      method: "PUT",
    });
  },
  cancel(id) {
    return request(`/Trade/${id}/cancel`, {
      method: "PUT",
    });
  },
};

export const categoryAPI = {
  getList() {
    return request("/Category");
  },
};

export const uploadAPI = {
  async upload(file) {
    const formData = new FormData();
    formData.append("file", file);
    
    const currentToken = localStorage.getItem("token") || "";
    
    try {
      const response = await fetch("http://172.19.229.149:5000/api/Upload", {
        method: "POST",
        headers: {
          "Authorization": `Bearer ${currentToken}`
        },
        body: formData
      });
      
      const data = await response.json();
      console.log("上传响应:", data);
      
      if (data.success && data.data && data.data.url) {
        return { success: true, url: data.data.url };
      }
      return { success: false, message: data.message || "上传失败" };
    } catch (error) {
      console.error("上传失败:", error);
      return { success: false, message: error.message };
    }
  }
};