# ApiGateway
 1.Ocelot是用.net Core实现的一款开源的网关,Ocelot其实就是一组按照顺序排列的.net core中间件。
 它可以具有身份验证，监控，负载均衡，缓存，请求分片与管理，静态响应处理等。
 API网关方式的核心要点是，所有的客户端和消费端都通过统一的网关接入微服务，在网关层处理所有的非业务功能。
 ApiGateway项目下的configuration.json有比较完整的Ocelot配置，有详细的注释说明。
 
 
2.ConsulCore简陋地封装了服务的注册与注销功能（以后还得继续完善）。
  Api_B项目下的ValuesController添加了一个Discovery方法用于测试服务发现并调用Api_A的方法。
