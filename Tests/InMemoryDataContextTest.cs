﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Simple1C.Interface;
using Simple1C.Tests.Helpers;
using Simple1C.Tests.Metadata1C.Документы;
using Simple1C.Tests.Metadata1C.Перечисления;
using Simple1C.Tests.Metadata1C.Справочники;

namespace Simple1C.Tests
{
    public class InMemoryDataContextTest : TestBase
    {
        private IDataContext dataContext;

        protected override void SetUp()
        {
            base.SetUp();
            dataContext = DataContextFactory.CreateInMemory(typeof(Контрагенты).Assembly);
        }

        [Test]
        public void EmptyStore()
        {
            var values = dataContext.Select<Контрагенты>().ToArray();
            Assert.That(values, Is.Empty);
        }

        [Test]
        public void NoBackSideEffects()
        {
            var контрагент = new Контрагенты
            {
                ИНН = "123"
            };
            dataContext.Save(контрагент);

            var контрагент2 = dataContext.Single<Контрагенты>(x => x.ИНН == "123");
            контрагент2.Наименование = "test-name";
            dataContext.Save(контрагент2);

            Assert.That(string.IsNullOrEmpty(контрагент.Наименование));
        }

        [Test]
        public void LoadNewestRevisionWhenAccessPropertyForTheFirstTime()
        {
            dataContext.Save(new ДоговорыКонтрагентов
            {
                Наименование = "test contract",
                Владелец = new Контрагенты
                {
                    Наименование = "test contractor name"
                }
            });

            var договор = dataContext.Single<ДоговорыКонтрагентов>();
            Assert.That(договор.Наименование, Is.EqualTo("test contract"));

            var контрагент = dataContext.Single<Контрагенты>();
            контрагент.Наименование = "test contractor changed name";
            dataContext.Save(контрагент);

            Assert.That(договор.Владелец.Наименование, Is.EqualTo("test contractor changed name"));
        }

        [Test]
        public void TakeLastRevisionOnQuery()
        {
            dataContext.Save(new ДоговорыКонтрагентов
                {
                    Владелец = new Контрагенты
                    {
                        Наименование = "test contractor name"
                    }
                });

            var контрагент = dataContext.Single<Контрагенты>();
            контрагент.Наименование = "test changed contractor name";
            dataContext.Save(контрагент);

            var договор = dataContext.Single<ДоговорыКонтрагентов>();
            Assert.That(договор.Владелец.Наименование, Is.EqualTo("test changed contractor name"));
        }

        [Test]
        public void CanSaveEmptyEntity()
        {
            var entity = new Контрагенты();
            dataContext.Save(entity);
            var values1 = dataContext.Select<Контрагенты>().ToArray();
            Assert.That(values1.Length, Is.EqualTo(1));
            Assert.That(values1[0].Код, Is.Not.Null);
            Assert.That(values1[0].Наименование, Is.Null);
            values1[0].Наименование = "changed";
            dataContext.Save(values1[0]);

            var values2 = dataContext.Select<Контрагенты>().ToArray();
            Assert.That(values2.Length, Is.EqualTo(1));
            Assert.That(values2[0].Код, Is.EqualTo(values1[0].Код));
            Assert.That(values2[0].Наименование, Is.EqualTo("changed"));
        }

        [Test]
        public void CanSetListForExistingDocument()
        {
            var entity = new ПоступлениеТоваровУслуг
            {
                Комментарий = "Тестовое наименование"
            };
            dataContext.Save(entity);

            var item = dataContext.Single<ПоступлениеТоваровУслуг>();
            item.Услуги.Add(new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
            {
                Содержание = "test-content"
            });
            dataContext.Save(item);

            Assert.That(dataContext.Single<ПоступлениеТоваровУслуг>().Услуги[0].Содержание,
                Is.EqualTo("test-content"));
        }

        [Test]
        public void SimpleCatalogSave()
        {
            var entity = new Контрагенты
            {
                Наименование = "Тестовое наименование"
            };
            dataContext.Save(entity);
            var values = dataContext.Select<Контрагенты>().ToArray();
            Assert.That(values.Length, Is.EqualTo(1));
            Assert.That(values[0].Код, Is.Not.Null);
            Assert.That(values[0].Наименование, Is.EqualTo("Тестовое наименование"));
        }

        [Test]
        public void CanUpdateEntity()
        {
            var contractor = new Контрагенты { Наименование = "Вася" };
            dataContext.Save(contractor);

            contractor.Наименование = "Ваня";
            Assert.That(dataContext.Select<Контрагенты>().Single().Наименование,
                Is.EqualTo("Вася"));

            dataContext.Save(contractor);
            Assert.That(dataContext.Select<Контрагенты>().Single().Наименование,
                Is.EqualTo("Ваня"));
        }

        [Test]
        public void CanSaveItemWithCode()
        {
            var contractor = new Контрагенты
            {
                Код = "test-code",
                Наименование = "Вася"
            };
            dataContext.Save(contractor);

            Assert.That(dataContext.Select<Контрагенты>().Single().Код,
                Is.EqualTo("test-code"));
        }

        [Test]
        public void SimpleDocumentSave()
        {
            var entity = new ПоступлениеТоваровУслуг
            {
                Комментарий = "Тестовое наименование"
            };
            dataContext.Save(entity);
            var values = dataContext.Select<ПоступлениеТоваровУслуг>().ToArray();
            Assert.That(values.Length, Is.EqualTo(1));
            Assert.That(values[0].Номер, Is.Not.Null);
            Assert.That(values[0].Комментарий, Is.EqualTo("Тестовое наименование"));

            values[0].Комментарий = "changed";
            Assert.That(values[0].Комментарий, Is.EqualTo("changed"));
            Assert.That(entity.Комментарий, Is.EqualTo("Тестовое наименование"));
            Assert.That(dataContext.Single<ПоступлениеТоваровУслуг>().Комментарий,
                Is.EqualTo("Тестовое наименование"));

            dataContext.Save(values[0]);
            Assert.That(values[0].Комментарий, Is.EqualTo("changed"));
            Assert.That(entity.Комментарий, Is.EqualTo("Тестовое наименование"));
            Assert.That(dataContext.Single<ПоступлениеТоваровУслуг>().Комментарий,
                Is.EqualTo("changed"));
        }

        [Test]
        public void CanUpdateExistingTableSectionItem()
        {
            var entity = new ПоступлениеТоваровУслуг
            {
                Комментарий = "Тестовое наименование",
                Услуги = new List<ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги>()
                {
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Содержание = "test-content"
                    }
                }
            };
            dataContext.Save(entity);
            var existing = dataContext.Select<ПоступлениеТоваровУслуг>().Single();
            existing.Услуги[0].Содержание = "changed-content1";
            Assert.That(dataContext.Select<ПоступлениеТоваровУслуг>().Single().Услуги[0].Содержание,
                Is.EqualTo("test-content"));
            dataContext.Save(existing);
            Assert.That(dataContext.Select<ПоступлениеТоваровУслуг>().Single().Услуги[0].Содержание,
                Is.EqualTo("changed-content1"));

            existing.Услуги[0].Содержание = "changed-content2";
            Assert.That(dataContext.Select<ПоступлениеТоваровУслуг>().Single().Услуги[0].Содержание,
                Is.EqualTo("changed-content1"));
            dataContext.Save(existing);
            Assert.That(dataContext.Select<ПоступлениеТоваровУслуг>().Single().Услуги[0].Содержание,
                Is.EqualTo("changed-content2"));
        }

        [Test]
        public void CanUpdateTableSectionItem()
        {
            var entity = new ПоступлениеТоваровУслуг
            {
                Комментарий = "Тестовое наименование",
                Услуги = new List<ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги>()
                {
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Содержание = "test-content"
                    }
                }
            };
            dataContext.Save(entity);
            Assert.That(dataContext.Select<ПоступлениеТоваровУслуг>().Single().Услуги[0].Содержание,
                Is.EqualTo("test-content"));
            entity.Услуги[0].Содержание = "changed-content";
            dataContext.Save(entity);
            Assert.That(dataContext.Select<ПоступлениеТоваровУслуг>().Single().Услуги[0].Содержание,
                Is.EqualTo("changed-content"));
        }

        [Test]
        public void CanDeleteTableSectionItem()
        {
            var entity = new ПоступлениеТоваровУслуг
            {
                Комментарий = "Тестовое наименование",
                Услуги = new List<ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги>()
                {
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Содержание = "test-content"
                    }
                }
            };
            dataContext.Save(entity);
            Assert.That(dataContext.Select<ПоступлениеТоваровУслуг>().Single().Услуги.Count,
                Is.EqualTo(1));
            entity.Услуги.RemoveAt(0);
            Assert.That(dataContext.Select<ПоступлениеТоваровУслуг>().Single().Услуги.Count,
                Is.EqualTo(1));
            dataContext.Save(entity);
            Assert.That(dataContext.Select<ПоступлениеТоваровУслуг>().Single().Услуги.Count,
                Is.EqualTo(0));
        }

        [Test]
        public void CanSaveDocumentWithTableSection()
        {
            var entity = new ПоступлениеТоваровУслуг
            {
                Комментарий = "Тестовое наименование",
                Услуги = new List<ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги>()
                {
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Содержание = "chair"
                    },
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Содержание = "table"
                    }
                }
            };
            Assert.That(entity.Услуги[0].Содержание, Is.EqualTo("chair"));
            Assert.That(entity.Услуги[1].Содержание, Is.EqualTo("table"));
            dataContext.Save(entity);
            var readEntity = dataContext.Select<ПоступлениеТоваровУслуг>().Single();
            Assert.That(readEntity.Услуги[0].Содержание, Is.EqualTo("chair"));
            Assert.That(readEntity.Услуги[1].Содержание, Is.EqualTo("table"));

            var t = readEntity.Услуги[0];
            readEntity.Услуги[0] = readEntity.Услуги[1];
            readEntity.Услуги[1] = t;

            Assert.That(entity.Услуги[0].Содержание, Is.EqualTo("chair"));
            Assert.That(entity.Услуги[1].Содержание, Is.EqualTo("table"));

            Assert.That(readEntity.Услуги[0].Содержание, Is.EqualTo("table"));
            Assert.That(readEntity.Услуги[1].Содержание, Is.EqualTo("chair"));

            var readEntity2 = dataContext.Select<ПоступлениеТоваровУслуг>().Single();
            Assert.That(readEntity2.Услуги[0].Содержание, Is.EqualTo("chair"));
            Assert.That(readEntity2.Услуги[1].Содержание, Is.EqualTo("table"));

            dataContext.Save(readEntity);
            var readEntity3 = dataContext.Select<ПоступлениеТоваровУслуг>().Single();
            Assert.That(readEntity3.Услуги[0].Содержание, Is.EqualTo("table"));
            Assert.That(readEntity3.Услуги[1].Содержание, Is.EqualTo("chair"));
        }

        [Test]
        public void SimpleInnerSave()
        {
            var counterparty = new Контрагенты
            {
                Наименование = "Тестовый контрагент"
            };
            var contract = new ДоговорыКонтрагентов
            {
                Наименование = "Тестовый договор",
                Владелец = counterparty
            };

            dataContext.Save(contract);
            var array = dataContext.Select<Контрагенты>().ToArray();
            Assert.That(array.Length, Is.EqualTo(1));
            Assert.That(array[0].Наименование, Is.EqualTo("Тестовый контрагент"));
        }

        [Test]
        public void UnionTypeWithAbstractEntityValue()
        {
            var counterparty = new Контрагенты
            {
                Наименование = "Тестовый контрагент"
            };
            var contract = new БанковскиеСчета
            {
                Наименование = "Тестовый счет",
                Владелец = counterparty
            };

            dataContext.Save(contract);
            var array = dataContext.Select<БанковскиеСчета>().ToArray();
            Assert.That(array.Length, Is.EqualTo(1));
            Assert.That(array[0].Владелец, Is.TypeOf<Контрагенты>());
            Assert.That(((Контрагенты)array[0].Владелец).Наименование,
                Is.EqualTo("Тестовый контрагент"));
        }

        [Test]
        public void CanReadUniqueIdentifierAfterSave()
        {
            var counterparty = new Контрагенты
            {
                Наименование = "Тестовый контрагент"
            };
            dataContext.Save(counterparty);
            Assert.IsNotNull(counterparty.УникальныйИдентификатор);
            Assert.That(counterparty.УникальныйИдентификатор.Value, Is.Not.EqualTo(Guid.Empty));

            var loadedCounterparty = dataContext
                .Select<Контрагенты>()
                .Single(x => x.УникальныйИдентификатор == counterparty.УникальныйИдентификатор.Value);
            Assert.That(loadedCounterparty.Наименование, Is.EqualTo("Тестовый контрагент"));

            loadedCounterparty.Наименование = "changed";
            dataContext.Save(loadedCounterparty);

            var loadedCounterparty2 = dataContext
                .Select<Контрагенты>()
                .Single(x => x.УникальныйИдентификатор == counterparty.УникальныйИдентификатор.Value);
            Assert.That(loadedCounterparty2.Наименование, Is.EqualTo("changed"));
        }

        [Test]
        public void UnionTypeWithEnumValue()
        {
            var contract = new БанковскиеСчета
            {
                Наименование = "Тестовый счет",
                Владелец = ВидыЛицензийАлкогольнойПродукции.Пиво
            };
            dataContext.Save(contract);
            var array = dataContext.Select<БанковскиеСчета>().ToArray();
            Assert.That(array.Length, Is.EqualTo(1));
            Assert.That(array[0].Владелец, Is.EqualTo(ВидыЛицензийАлкогольнойПродукции.Пиво));
        }

        [Test]
        public void CanReadValueTypes()
        {
            var acocunt = new БанковскиеСчета
            {
                ДатаЗакрытия = new DateTime(2016, 6, 21)
            };
            dataContext.Save(acocunt);

            var account = dataContext.Single<БанковскиеСчета>();
            Assert.That(account.ДатаЗакрытия, Is.EqualTo(new DateTime(2016, 6, 21)));
        }

        [Test]
        public void NestedObjectNullException()
        {
            var договор = new ДоговорыКонтрагентов
            {
                Владелец = null
            };
            dataContext.Save(договор);
            Assert.That(договор.УникальныйИдентификатор, Is.Not.Null);
            var договоры = dataContext.Select<ДоговорыКонтрагентов>()
                .Where(x => x.УникальныйИдентификатор == договор.УникальныйИдентификатор)
                .ToArray();
            Assert.That(договоры.Length, Is.EqualTo(1));
            Assert.That(договоры[0].Владелец, Is.Null);
        }
    }
}