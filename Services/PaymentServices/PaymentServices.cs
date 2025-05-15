﻿using System.Linq;
using System.Security.Claims;
using AutoMapper;
using Blink_API.DTOs.CartDTOs;

using Blink_API.DTOs.OrdersDTO;

using Blink_API.DTOs.PaymentCart;
using Blink_API.Errors;
using Blink_API.Models;
using Blink_API.Services.OrderServicees;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Stripe;
using Stripe.Climate;
using Stripe.Issuing;


namespace Blink_API.Services.PaymentServices
{
    public class PaymentServices : IPaymentServices
    {
        private readonly IConfiguration _configuration;

        private readonly UnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PaymentServices(IConfiguration configuration, UnitOfWork unitOfWork
            , IMapper mapper )
      
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }


        #region Edit Finish
        public async Task<ApiResponse<CartPaymentDTO>> CreatePaymentIntentAsync(string userId, decimal amount)
        {
            try
            {
                StripeConfiguration.ApiKey = _configuration["StripeSettings:SecretKey"];

                var cart = await _unitOfWork.CartRepo.GetByUserId(userId);
                if (cart == null || !cart.CartDetails.Any())
                {
                    return new ApiResponse<CartPaymentDTO>(404, "Cart is empty or not found.");
                }

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100),
                    Currency = "usd",
                    PaymentMethodTypes = new List<string> { "card" }
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                var payment = new Payment
                {
                    Method = "card",
                    PaymentDate = DateTime.UtcNow,
                    PaymentStatus = "pending",
                    PaymentIntentId = paymentIntent.Id
                };

                _unitOfWork.PaymentRepository.Add(payment);
                await _unitOfWork.CompleteAsync();

                var result = new CartPaymentDTO
                {
                    PaymentIntentId = paymentIntent.Id,
                    ClientSecret = paymentIntent.ClientSecret
                };

                return new ApiResponse<CartPaymentDTO>(200, "Payment intent created successfully", result);
            }
            catch (Exception ex)
            {
                return new ApiResponse<CartPaymentDTO>(500, $"Something went wrong: {ex.Message}");
            }
        }





        #endregion




        #region Finish Payment

        //public async Task<CartPaymentDTO?> CreatePaymentIntent( string userId ,decimal amount)
        //{

        //    StripeConfiguration.ApiKey = _configuration["StripeSettings:SecretKey"];

        //    // 1. Get cart
        //    var cart = await _unitOfWork.CartRepo.GetByUserId(userId);
        //    if (cart == null || !cart.CartDetails.Any())
        //        throw new Exception("Cart is empty or not found.");


        //    #region Calc Price And Inventory
        //    var OrderSubTotal = cart.CartDetails.Sum(cd => cd.Product.StockProductInventories.Average(spi => spi.StockUnitPrice)); 
        //    var OrderTax = OrderSubTotal * 0.14m; 
        //    var ShippingCost = 10;
        //    var OrderTotalAmount = OrderSubTotal + OrderTax + ShippingCost;


        //    #endregion


        //    var service = new PaymentIntentService();
        //    PaymentIntent? paymentIntent = null;


        //    var options = new PaymentIntentCreateOptions
        //    {
        //        Amount = (long)(amount * 100),
        //        Currency = "usd",
        //        PaymentMethodTypes = new List<string> { "card" },

        //    };
        //    paymentIntent = await service.CreateAsync(options);

        //    string? method = cart.OrderHeader?.Payment?.Method ?? "card";

        //    var newPayment = new Payment
        //    {
        //        Method = method,
        //        PaymentDate = DateTime.UtcNow,
        //        PaymentStatus = "pending",
        //        PaymentIntentId = paymentIntent.Id,
        //    };
        //     _unitOfWork.PaymentRepository.Add(newPayment);

        //    var existingOrder = await _unitOfWork.OrderRepo.GetById(cart.CartId);



        //    if (existingOrder == null)
        //    {
        //        cart.OrderHeader = new OrderHeader()
        //        {
        //            PaymentId = newPayment.PaymentId,
        //            PaymentIntentId = paymentIntent.Id,
        //            CartId = cart.CartId,
        //            OrderDate = DateTime.UtcNow,
        //            OrderStatus = "shipped",
        //            OrderSubtotal = OrderSubTotal,
        //            OrderTax = OrderTax,
        //            OrderShippingCost = 10,
        //            OrderTotalAmount = OrderTotalAmount,
        //            Payment = newPayment
        //        };
        //        _unitOfWork.OrderRepo.Add(cart.OrderHeader);
        //    }
        //    else
        //    {
        //        existingOrder.PaymentId = newPayment.PaymentId;

        //        _unitOfWork.OrderRepo.Update(existingOrder);

        //    }
        //        await _unitOfWork.CompleteAsync();
        //    if (cart.CartDetails != null && cart.CartDetails.Any())
        //    {
        //        foreach (var cartDetail in cart.CartDetails)
        //        {
        //            var orderDetail = new OrderDetail
        //            {
        //                OrderHeaderId = cart.OrderHeader.OrderHeaderId,
        //                ProductId = cartDetail.ProductId,
        //                SellQuantity = cartDetail.Quantity,
        //                SellPrice = cartDetail.Product.StockProductInventories.Average(spi => spi.StockUnitPrice)
        //            };

        //            _unitOfWork.OrderDetailRepo.Add(orderDetail);
        //        }
        //    }


        //    cart.OrderHeader.PaymentIntentId = paymentIntent.Id;




        //    var mappedCart = _mapper.Map<CartPaymentDTO>(cart);
        //    mappedCart.PaymentIntentId = paymentIntent.Id;
        //    mappedCart.ClientSecret = paymentIntent.ClientSecret;

        //    cart.IsDeleted = true;
        //    _unitOfWork.CartRepo.Update(cart);
        //    await _unitOfWork.CompleteAsync();

        //    return mappedCart;
        //}
        #endregion






        //public async Task MonitorPaymentStatus(string paymentIntentId, CreateOrderDTO createOrderDTO)
        //{
        //    bool isPaid = false;

        //    for (int i = 0; i < 3; i++)
        //    {
        //        isPaid = await PollPaymentStatus(paymentIntentId);

        //        if (isPaid)
        //        {
        //            break;
        //        }

        //        await Task.Delay(TimeSpan.FromMinutes(5));
        //    }

        //    if (isPaid)
        //    {
        //        // Create the order after successful payment
        //        await CreateOrder(createOrderDTO); 
        //        await UpdatePaymentIntentToSucceededOrFailed(paymentIntentId, true);
        //    }
        //    else
        //    {
        //        await UpdatePaymentIntentToSucceededOrFailed(paymentIntentId, false);
        //    }
        //}


        //public async Task CreateOrder(CreateOrderDTO createOrderDTO)
        //{
        //    //  save the order to the database
        //    var orderServices = _orderServices.Value;
        //   await orderServices.CreateOrderAsync(createOrderDTO);
        //}

        //public async Task<CartPaymentDTO> HandlePaymentAsync(int cartId, string userId, CreateOrderDTO createOrderDTO)
        //{
        //    var cart = await _unitOfWork.CartRepo.GetByUserId(userId);
        //    var paymentIntentId = cart.OrderHeader.PaymentIntentId;

        //    await MonitorPaymentStatus(paymentIntentId, createOrderDTO); // Pass the createOrderDTO here

        //    var cartPaymentDTO = _mapper.Map<CartPaymentDTO>(cart);
        //    return cartPaymentDTO;
        //}


    }
}
