<template>
  <div class="login-container">
    <el-card class="box-card">
      <div class="login-body">
        <div class="login-title">后台管理</div>
        <el-form ref="form" :model="userForm">
          <el-input placeholder="请输入管理员账号" v-model="userForm.accountNumber" class="login-input">
            <template slot="prepend">
              <div class="el-icon-user-solid"></div>
            </template>
          </el-input>
          <el-input placeholder="请输入管理员密码" v-model="userForm.adminPassword" class="login-input"
                    @keyup.enter.native="login" show-password>
            <template slot="prepend">
              <div class="el-icon-lock"></div>
            </template>
          </el-input>
          <div class="login-submit">
            <el-button type="primary" @click="login">登录</el-button>
          </div>
          <div class="other-submit">
            <router-link to="/login" class="sign-in-text">学生登录</router-link>
          </div>
        </el-form>
      </div>
    </el-card>
  </div>
</template>

<script>
export default {
  name: "login-admin",
  data() {
    return {
      userForm: {
        accountNumber: '',
        adminPassword: ''
      }
    };
  },
  methods: {
    login() {
      // 适配新 API 参数格式
      this.$api.adminLogin({
        username: this.userForm.accountNumber,   // 字段名改为 username
        password: this.userForm.adminPassword    // 字段名改为 password
      }).then(res => {
        console.log(res);
        if (res.status_code === 1) {
          // 保存 token 和管理员信息
          if (res.data && res.data.token) {
            localStorage.setItem('adminToken', res.data.token);
            localStorage.setItem('adminInfo', JSON.stringify(res.data.userInfo));
          }
          this.$sta.isLogin = true;
          this.$sta.adminName = res.data.userInfo?.username || '管理员';
          this.$router.replace({path: '/platform-admin'});
        } else {
          this.$message.error(res.msg || '登录失败，账号或密码错误！');
        }
      }).catch(e => {
        console.log(e);
        this.$message.error('登录失败，请检查网络！');
      });
    }
  }
}
</script>

<style scoped>
.login-container {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100vh;
  width: 100%;
  background-color: #f1f1f1;
}
.login-body {
  padding: 30px;
  width: 400px;
  height: 100%;
}
.login-title {
  padding-bottom: 30px;
  text-align: center;
  font-weight: 600;
  font-size: 20px;
  color: #409EFF;
  cursor: pointer;
}
.login-input {
  margin-bottom: 20px;
}
.login-submit {
  display: flex;
  justify-content: center;
}
.sign-in-text {
  color: #409EFF;
  font-size: 16px;
  text-decoration: none;
  line-height:28px;
}
.other-submit{
  display:flex;
  justify-content: space-between;
  margin-top: 10px;
}
</style>